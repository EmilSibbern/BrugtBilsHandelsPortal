namespace BrugtBilsHandelsPortal.DTOs.Comments;

public class CommentDto
{
    public long Id { get; init; }

    public int AnnouncementId { get; init; }

    public int UserId { get; init; }

    public string Username { get; init; } = string.Empty;

    public string Text { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }

    public long? ParentCommentId { get; init; }

    public bool IsDeleted { get; init; }

    public IReadOnlyList<CommentDto> Replies { get; init; }
        = Array.Empty<CommentDto>();
}