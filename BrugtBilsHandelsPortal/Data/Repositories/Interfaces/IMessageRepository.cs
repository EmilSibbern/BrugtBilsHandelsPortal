using BrugtBilsHandelsPortal.DTOs.Messages;
using BrugtBilsHandelsPortal.Models.Entities;

namespace BrugtBilsHandelsPortal.Data.Repositories.Interfaces;

/// <summary>
/// Definerer persistens og deltagerbegrænset læsning
/// af private beskeder.
/// </summary>
public interface IMessageRepository
{
    Task<long> CreateAsync(Message message);

    Task<MessageDto?> GetByIdForParticipantAsync(long messageId, int currentUserId);

    Task<IReadOnlyList<MessageDto>> GetInboxAsync(int receiverUserId);

    Task<IReadOnlyList<MessageDto>> GetConversationAsync(int announcementId, int firstUserId, int secondUserId);

    Task<int> MarkConversationAsReadAsync(int announcementId, int receiverUserId, int senderUserId, DateTime readAt);

    Task<int> CountSentSinceAsync(int senderUserId, DateTime since);
}