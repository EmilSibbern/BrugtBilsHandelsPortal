using System.ComponentModel.DataAnnotations;
using BrugtBilsHandelsPortal.Data.Repositories.Interfaces;
using BrugtBilsHandelsPortal.DTOs.Announcements;
using BrugtBilsHandelsPortal.DTOs.Messages;
using BrugtBilsHandelsPortal.Models.Entities;
using BrugtBilsHandelsPortal.Services.Implementations;
using BrugtBilsHandelsPortal.Tests.Fakes;
using Xunit;

namespace BrugtBilsHandelsPortal.Tests.Services;

public class MessageServiceTests
{
    [Fact]
    [Trait("UserStory", "US12")]
    public async Task SendAsync_AsGuest_IsRejectedBeforeRepositoryCalls()
    {
        var repository = new FakeMessageRepository();
        var service = CreateService(repository, currentUserId: null);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.SendAsync(CreateRequest()));

        Assert.Equal(0, repository.CreateCallCount);
        Assert.Equal(0, repository.CountSentCallCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [Trait("UserStory", "US12")]
    public async Task SendAsync_WithBlankText_IsRejected(string text)
    {
        var repository = new FakeMessageRepository();
        var service = CreateService(repository, currentUserId: 9);

        await Assert.ThrowsAsync<ValidationException>(
            () => service.SendAsync(CreateRequest(text)));

        Assert.Equal(0, repository.CreateCallCount);
    }

    [Fact]
    [Trait("UserStory", "US12")]
    public async Task SendAsync_WithMoreThanTwoThousandCharacters_IsRejected()
    {
        var repository = new FakeMessageRepository();
        var service = CreateService(repository, currentUserId: 9);

        await Assert.ThrowsAsync<ValidationException>(
            () => service.SendAsync(
                CreateRequest(new string('a', 2001))));

        Assert.Equal(0, repository.CreateCallCount);
    }

    [Fact]
    [Trait("UserStory", "US12")]
    public async Task SendAsync_UsesCurrentUserAndAnnouncementOwner()
    {
        var repository = new FakeMessageRepository();
        var service = CreateService(
            repository,
            currentUserId: 9,
            sellerUserId: 7);

        var result = await service.SendAsync(
            CreateRequest("  Er bilen stadig til salg?  "));

        Assert.NotNull(repository.CreatedMessage);
        Assert.Equal(9, repository.CreatedMessage.SenderUserId);
        Assert.Equal(7, repository.CreatedMessage.ReceiverUserId);
        Assert.Equal(42, repository.CreatedMessage.AnnouncementId);
        Assert.Equal(
            "Er bilen stadig til salg?",
            repository.CreatedMessage.Text);
        Assert.False(repository.CreatedMessage.IsRead);
        Assert.Null(repository.CreatedMessage.ReadAt);

        Assert.Equal(1, result.Id);
        Assert.Equal(9, result.SenderUserId);
        Assert.Equal(7, result.ReceiverUserId);
        Assert.False(result.IsRead);
    }

    [Fact]
    [Trait("UserStory", "US12")]
    public async Task SendAsync_ToOwnAnnouncement_IsRejected()
    {
        var repository = new FakeMessageRepository();
        var service = CreateService(
            repository,
            currentUserId: 7,
            sellerUserId: 7);

        await Assert.ThrowsAsync<ValidationException>(
            () => service.SendAsync(CreateRequest()));

        Assert.Equal(0, repository.CreateCallCount);
    }

    [Fact]
    [Trait("UserStory", "US12")]
    public async Task SendAsync_ForInactiveAnnouncement_IsRejected()
    {
        var repository = new FakeMessageRepository();
        var service = CreateService(
            repository,
            currentUserId: 9,
            announcementIsActive: false);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SendAsync(CreateRequest()));

        Assert.Equal(0, repository.CreateCallCount);
    }

    [Fact]
    [Trait("UserStory", "US12")]
    public async Task SendAsync_WhenRateLimitReached_IsRejected()
    {
        var repository = new FakeMessageRepository
        {
            SentSinceCount = 10
        };
        var service = CreateService(repository, currentUserId: 9);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SendAsync(CreateRequest()));

        Assert.Equal(1, repository.CountSentCallCount);
        Assert.Equal(0, repository.CreateCallCount);
    }

    [Fact]
    [Trait("UserStory", "US12")]
    public async Task GetInboxAsync_AsGuest_IsRejected()
    {
        var repository = new FakeMessageRepository();
        var service = CreateService(repository, currentUserId: null);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.GetInboxAsync());

        Assert.Equal(0, repository.GetInboxCallCount);
    }

    [Fact]
    [Trait("UserStory", "US12")]
    public async Task GetInboxAsync_UsesCurrentUserAsReceiver()
    {
        var repository = new FakeMessageRepository();
        repository.Messages[2] = CreateMessage(
            id: 2,
            senderUserId: 9,
            receiverUserId: 7);
        repository.Messages[3] = CreateMessage(
            id: 3,
            senderUserId: 9,
            receiverUserId: 99);

        var service = CreateService(repository, currentUserId: 7);

        var inbox = await service.GetInboxAsync();

        var message = Assert.Single(inbox);
        Assert.Equal(2, message.Id);
        Assert.Equal(7, message.ReceiverUserId);
        Assert.Equal(7, repository.LastInboxUserId);
    }

    [Fact]
    [Trait("UserStory", "US12")]
    public async Task GetByIdAsync_AdministratorWhoIsNotParticipant_GetsNoMessage()
    {
        var repository = new FakeMessageRepository();
        repository.Messages[2] = CreateMessage(
            id: 2,
            senderUserId: 9,
            receiverUserId: 7);

        var service = CreateService(
            repository,
            currentUserId: 1,
            isAdministrator: true);

        var result = await service.GetByIdAsync(2);

        Assert.Null(result);
        Assert.Equal(1, repository.LastParticipantUserId);
    }

    private static MessageService CreateService(
        FakeMessageRepository messageRepository,
        int? currentUserId,
        int sellerUserId = 7,
        bool announcementIsActive = true,
        bool isAdministrator = false)
    {
        var announcementRepository = new FakeAnnouncementRepository();
        announcementRepository.Details[42] =
            new AnnouncementDetailsDto
            {
                Id = 42,
                IsActive = announcementIsActive,
                SellerUserId = sellerUserId,
                SellerUsername = "sælger",
                Title = "Skoda Octavia"
            };

        return new MessageService(
            messageRepository,
            announcementRepository,
            new FakeCurrentUserService
            {
                UserId = currentUserId,
                IsAdministrator = isAdministrator
            });
    }

    private static SendMessageDto CreateRequest(
        string text = "Er bilen stadig til salg?")
    {
        return new SendMessageDto
        {
            AnnouncementId = 42,
            Text = text
        };
    }

    private static MessageDto CreateMessage(
        long id,
        int senderUserId,
        int receiverUserId,
        string text = "Er bilen stadig til salg?")
    {
        return new MessageDto
        {
            Id = id,
            AnnouncementId = 42,
            AnnouncementTitle = "Skoda Octavia",
            SenderUserId = senderUserId,
            SenderUsername = "afsender",
            ReceiverUserId = receiverUserId,
            ReceiverUsername = "modtager",
            Text = text,
            CreatedAt = new DateTime(2026, 8, 24),
            IsRead = false
        };
    }

    private sealed class FakeMessageRepository : IMessageRepository
    {
        public Dictionary<long, MessageDto> Messages { get; } = [];
        public Message? CreatedMessage { get; private set; }
        public int SentSinceCount { get; set; }
        public int CreateCallCount { get; private set; }
        public int CountSentCallCount { get; private set; }
        public int GetInboxCallCount { get; private set; }
        public int? LastInboxUserId { get; private set; }
        public int? LastParticipantUserId { get; private set; }

        public Task<long> CreateAsync(Message message)
        {
            CreateCallCount++;
            CreatedMessage = message;
            const long messageId = 1;
            Messages[messageId] = CreateMessage(
                messageId,
                message.SenderUserId,
                message.ReceiverUserId,
                message.Text);
            return Task.FromResult(messageId);
        }

        public Task<MessageDto?> GetByIdForParticipantAsync(
            long messageId,
            int currentUserId)
        {
            LastParticipantUserId = currentUserId;
            Messages.TryGetValue(messageId, out var message);
            var canRead = message is not null
                && (message.SenderUserId == currentUserId
                    || message.ReceiverUserId == currentUserId);
            return Task.FromResult(canRead ? message : null);
        }

        public Task<IReadOnlyList<MessageDto>> GetInboxAsync(
            int receiverUserId)
        {
            GetInboxCallCount++;
            LastInboxUserId = receiverUserId;
            IReadOnlyList<MessageDto> inbox = Messages.Values
                .Where(message =>
                    message.ReceiverUserId == receiverUserId)
                .ToList();
            return Task.FromResult(inbox);
        }

        public Task<int> CountSentSinceAsync(
            int senderUserId,
            DateTime since)
        {
            CountSentCallCount++;
            return Task.FromResult(SentSinceCount);
        }
    }
}
