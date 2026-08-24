using System.ComponentModel.DataAnnotations;

namespace BrugtBilsHandelsPortal.DTOs.Comments;

public class CreateCommentDto
{
    [Range(1, int.MaxValue)]
    public int AnnouncementId { get; init; }

    [Required]
    [StringLength(1500, MinimumLength = 1)]
    public string Text { get; init; } = string.Empty;

    public long? ParentCommentId { get; init; }
}