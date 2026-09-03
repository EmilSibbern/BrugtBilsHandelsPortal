using System.ComponentModel.DataAnnotations;
using BrugtBilsHandelsPortal.Data.Repositories.Interfaces;
using BrugtBilsHandelsPortal.DTOs.Messages;
using BrugtBilsHandelsPortal.Models.Entities;
using BrugtBilsHandelsPortal.Security;
using BrugtBilsHandelsPortal.Services.Interfaces;

namespace BrugtBilsHandelsPortal.Services.Implementations;

/// <summary>Håndhæver deltageradgang, inputregler og rate limit for private beskeder.</summary>
public class MessageService : IMessageService
{
    // En enkel databasebaseret rate limit fungerer på tværs af flere serverinstanser.
    private const int MaxMessagesPerMinute = 10;

    private readonly IMessageRepository _messageRepository;
    private readonly IAnnouncementRepository _announcementRepository;
    private readonly ICurrentUserService _currentUserService;

    public MessageService(IMessageRepository messageRepository, IAnnouncementRepository announcementRepository, ICurrentUserService currentUserService)
    {
        _messageRepository = messageRepository;
        _announcementRepository = announcementRepository;
        _currentUserService = currentUserService;
    }

    public async Task<MessageDto> SendAsync(SendMessageDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateRequest(request);

        var senderUserId = await _currentUserService.GetUserIdAsync();

        if (senderUserId is null)
        {
            throw new UnauthorizedAccessException("Du skal være logget ind for at sende beskeder.");
        }

        var announcement = await _announcementRepository.GetActiveByIdAsync(request.AnnouncementId);

        if (announcement is null)
        {
            throw new InvalidOperationException("Annoncen findes ikke eller er ikke længere aktiv.");
        }

        var receiverUserId = announcement.SellerUserId;

        if (receiverUserId == senderUserId.Value)
        {
            throw new ValidationException("Du kan ikke sende en besked til dig selv.");
        }

        var rateLimitStart = DateTime.UtcNow.AddMinutes(-1);

        var recentlySentCount = await _messageRepository.CountSentSinceAsync(senderUserId.Value, rateLimitStart);

        if (recentlySentCount >= MaxMessagesPerMinute)
        {
            throw new InvalidOperationException("Du har sendt for mange beskeder. Vent et øjeblik og prøv igen.");
        }

        var message = new Message
        {
            AnnouncementId = request.AnnouncementId,
            SenderUserId = senderUserId.Value,
            ReceiverUserId = receiverUserId,
            Text = request.Text.Trim()
        };

        var messageId = await _messageRepository.CreateAsync(message);

        var createdMessage = await _messageRepository.GetByIdForParticipantAsync(messageId, senderUserId.Value);

        if (createdMessage is null)
        {
            throw new InvalidOperationException("Beskeden blev oprettet, men kunne ikke genindlæses.");
        }

        return createdMessage;
    }

    public async Task<IReadOnlyList<MessageDto>> GetInboxAsync()
    {
        var currentUserId = await _currentUserService.GetUserIdAsync();

        if (currentUserId is null)
        {
            throw new UnauthorizedAccessException("Du skal være logget ind for at se beskeder.");
        }

        return await _messageRepository.GetInboxAsync(currentUserId.Value);
    }

    public async Task<MessageDto?> GetByIdAsync(long messageId)
    {
        if (messageId <= 0)
        {
            return null;
        }

        var currentUserId = await _currentUserService.GetUserIdAsync();

        if (currentUserId is null)
        {
            throw new UnauthorizedAccessException("Du skal være logget ind for at se beskeder.");
        }

        return await _messageRepository.GetByIdForParticipantAsync(messageId, currentUserId.Value);
    }

    public async Task<ConversationDto?> GetConversationAsync(long messageId)
{
    if (messageId <= 0)
    {
        return null;
    }

    var currentUserId = await _currentUserService.GetUserIdAsync();

    if (currentUserId is null)
    {
        throw new UnauthorizedAccessException("Du skal være logget ind for at se samtaler.");
    }

    var selectedMessage = await _messageRepository.GetByIdForParticipantAsync(messageId, currentUserId.Value);

    if (selectedMessage is null)
    {
        return null;
    }

    var otherUserId = selectedMessage.SenderUserId == currentUserId.Value ? selectedMessage.ReceiverUserId : selectedMessage.SenderUserId;

    var otherUsername = selectedMessage.SenderUserId == currentUserId.Value ? selectedMessage.ReceiverUsername : selectedMessage.SenderUsername;

    await _messageRepository.MarkConversationAsReadAsync(selectedMessage.AnnouncementId, currentUserId.Value, otherUserId, DateTime.UtcNow);

    var messages = await _messageRepository.GetConversationAsync(selectedMessage.AnnouncementId, currentUserId.Value, otherUserId);

    return new ConversationDto
    {
        AnnouncementId = selectedMessage.AnnouncementId,

        AnnouncementTitle = selectedMessage.AnnouncementTitle,

        OtherUserId = otherUserId,

        OtherUsername = otherUsername,

        Messages = messages
    };
}

public async Task<MessageDto> ReplyAsync(long messageId, string text)
{
    ValidateText(text);

    if (messageId <= 0)
    {
        throw new ValidationException("Samtalen er ugyldig.");
    }

    var currentUserId = await _currentUserService.GetUserIdAsync();

    if (currentUserId is null)
    {
        throw new UnauthorizedAccessException("Du skal være logget ind for at svare.");
    }

    var selectedMessage = await _messageRepository.GetByIdForParticipantAsync(messageId, currentUserId.Value);

    if (selectedMessage is null)
    {
        throw new InvalidOperationException("Samtalen findes ikke, eller du har ikke adgang til den.");
    }

    var receiverUserId = selectedMessage.SenderUserId == currentUserId.Value ? selectedMessage.ReceiverUserId : selectedMessage.SenderUserId;

    var rateLimitStart = DateTime.UtcNow.AddMinutes(-1);

    var recentlySentCount = await _messageRepository.CountSentSinceAsync(currentUserId.Value, rateLimitStart);

    if (recentlySentCount >= MaxMessagesPerMinute)
    {
        throw new InvalidOperationException("Du har sendt for mange beskeder. Vent et øjeblik og prøv igen.");
    }

    var reply = new Message
    {
        AnnouncementId = selectedMessage.AnnouncementId,

        SenderUserId = currentUserId.Value,

        ReceiverUserId = receiverUserId,

        Text = text.Trim()
    };

    var createdMessageId = await _messageRepository.CreateAsync(reply);

    var createdMessage = await _messageRepository.GetByIdForParticipantAsync(createdMessageId, currentUserId.Value);

    if (createdMessage is null)
    {
        throw new InvalidOperationException("Svaret blev oprettet, men kunne ikke genindlæses.");
    }

    return createdMessage;
}

    private static void ValidateRequest(SendMessageDto request)
    {
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, new ValidationContext(request), validationResults, validateAllProperties: true);

        if (!isValid)
        {
            throw new ValidationException(validationResults.First().ErrorMessage ?? "Beskeden er ugyldig.");
        }

        if (string.IsNullOrWhiteSpace(request.Text))
        {
            throw new ValidationException("Beskeden må ikke være tom.");
        }
    }

    private static void ValidateText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ValidationException("Beskeden må ikke være tom.");
        }

        if (text.Trim().Length > 2000)
        {
            throw new ValidationException("Beskeden må højst være 2000 tegn.");
        }
    }
}