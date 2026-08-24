using BrugtBilsHandelsPortal.DTOs.Messages;

namespace BrugtBilsHandelsPortal.Services.Interfaces;

public interface IMessageService
{
    Task<MessageDto> SendAsync(
        SendMessageDto request);

    Task<IReadOnlyList<MessageDto>> GetInboxAsync();

    Task<MessageDto?> GetByIdAsync(
        long messageId);
}