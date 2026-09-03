using System.ComponentModel.DataAnnotations;

namespace BrugtBilsHandelsPortal.DTOs.Announcements;

/// <summary>
/// Filtre og sideinddeling til administratorens
/// annonceoversigt.
/// </summary>
public class AdminAnnouncementSearchDto
{
    [StringLength(100, ErrorMessage = "Søgningen må højst være 100 tegn.")]
    public string? Query { get; set; }

    [StringLength(100, ErrorMessage = "Ejersøgningen må højst være 100 tegn.")]
    public string? OwnerQuery { get; set; }

    public bool? IsActive { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Siden er ugyldig.")]
    public int Page { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "Der kan højst vises 100 annoncer ad gangen.")]
    public int PageSize { get; set; } = 20;
}