using BrugtBilsHandelsPortal.DTOs.Messages;

namespace BrugtBilsHandelsPortal.Services.Interfaces;

/// <summary>
/// Håndterer private beskeder mellem køber og sælger
/// med adgangskontrol og rate limiting.
/// </summary>
public interface IMessageService
{
    /// <summary>
    /// Sender den første besked fra den aktuelle bruger
    /// til annoncens ejer.
    /// </summary>
    Task<MessageDto> SendAsync(SendMessageDto request);

    /// <summary>
    /// Henter den aktuelle brugers modtagne beskeder.
    /// </summary>
    Task<IReadOnlyList<MessageDto>> GetInboxAsync();

    /// <summary>
    /// Henter en besked, hvis den aktuelle bruger
    /// deltager i samtalen.
    /// </summary>
    Task<MessageDto?> GetByIdAsync(long messageId);

    /// <summary>
    /// Henter hele samtalen omkring en besked.
    /// Modtagne beskeder i samtalen markeres samtidig som læst.
    /// </summary>
    Task<ConversationDto?> GetConversationAsync(long messageId);

    /// <summary>
    /// Sender et svar i en eksisterende samtale.
    /// </summary>
    Task<MessageDto> ReplyAsync(long messageId, string text);
}