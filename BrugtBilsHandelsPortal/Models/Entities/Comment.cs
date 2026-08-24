namespace BrugtBilsHandelsPortal.Models.Entities;

public class Comment
{
    public long Id { get; set; }

    public int AnnouncementId { get; set; }

    public int UserId { get; set; }

    public string Text { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public long? ParentCommentId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int? DeletedByUserId { get; set; }
}