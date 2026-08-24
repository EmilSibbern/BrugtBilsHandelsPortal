using BrugtBilsHandelsPortal.DTOs.Comments;

namespace BrugtBilsHandelsPortal.Services.Interfaces;

public interface ICommentService
{
    Task<IReadOnlyList<CommentDto>> GetForAnnouncementAsync(
        int announcementId);

    Task<CommentDto> CreateAsync(
        CreateCommentDto request);

    Task DeleteAsync(
        long commentId);
}