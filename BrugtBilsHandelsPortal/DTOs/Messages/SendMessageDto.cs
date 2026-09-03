using System.ComponentModel.DataAnnotations;

namespace BrugtBilsHandelsPortal.DTOs.Messages;

public class SendMessageDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Annoncen er ugyldig.")]
    public int AnnouncementId { get; init; }

    [Required(ErrorMessage = "Beskeden må ikke være tom.")]
    [StringLength(2000, MinimumLength = 1, ErrorMessage = "Beskeden skal være mellem 1 og 2000 tegn.")]
    public string Text { get; init; } = string.Empty;
}