using BrugtBilsHandelsPortal.DTOs.Messages;
using BrugtBilsHandelsPortal.Models.Entities;

namespace BrugtBilsHandelsPortal.Data.Repositories.Interfaces;

public interface IMessageRepository
{
    Task<long> CreateAsync(
        Message message);

    Task<MessageDto?> GetByIdForParticipantAsync(
        long messageId,
        int currentUserId);

    Task<IReadOnlyList<MessageDto>> GetInboxAsync(
        int receiverUserId);

    Task<int> CountSentSinceAsync(
        int senderUserId,
        DateTime since);
}