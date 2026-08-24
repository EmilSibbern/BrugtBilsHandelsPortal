using BrugtBilsHandelsPortal.DTOs.Messages;

namespace BrugtBilsHandelsPortal.Services.Interfaces;

/// <summary>Håndterer private beskeder mellem køber og sælger med adgangskontrol og rate limit.</summary>
public interface IMessageService
{
    /// <summary>Sender en besked fra den aktuelle bruger til annoncens ejer.</summary>
    Task<MessageDto> SendAsync(
        SendMessageDto request);

    /// <summary>Henter den aktuelle brugers modtagne beskeder.</summary>
    Task<IReadOnlyList<MessageDto>> GetInboxAsync();

    /// <summary>Henter en besked, hvis den aktuelle bruger deltager i samtalen.</summary>
    Task<MessageDto?> GetByIdAsync(
        long messageId);
}
