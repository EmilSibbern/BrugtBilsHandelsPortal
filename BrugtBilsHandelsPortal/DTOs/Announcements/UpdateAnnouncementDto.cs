using System.ComponentModel.DataAnnotations;

namespace BrugtBilsHandelsPortal.DTOs.Announcements;

public class UpdateAnnouncementDto
    : CreateAnnouncementDto
{
    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "Annoncen er ugyldig.")]
    public int AnnouncementId { get; set; }

    public DateTime? ExpectedUpdatedAt { get; set; }

    public bool CanManageImages { get; set; }

}
