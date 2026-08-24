using BrugtBilsHandelsPortal.DTOs.Comments;
using BrugtBilsHandelsPortal.Models.Entities;

namespace BrugtBilsHandelsPortal.Data.Repositories.Interfaces;

/// <summary>Definerer persistensoperationer for kommentartråde og soft delete.</summary>
public interface ICommentRepository
{
    Task<IReadOnlyList<CommentDto>> GetForActiveAnnouncementAsync(
        int announcementId);

    Task<CommentDto?> GetByIdAsync(
        long commentId);

    Task<long> CreateAsync(
        Comment comment);

    Task<bool> SoftDeleteAsync(
        long commentId,
        int deletedByUserId,
        DateTime deletedAt);
}
