using System.ComponentModel.DataAnnotations;

namespace BrugtBilsHandelsPortal.DTOs.Announcements;

public class AnnouncementSearchDto
{
    [StringLength(
        100,
        ErrorMessage = "Søgningen må højst være 100 tegn.")]
    public string? Query { get; set; }

    public int? BrandId { get; set; }

    [Range(
        0,
        int.MaxValue,
        ErrorMessage = "Minimumsprisen må ikke være negativ.")]
    public int? MinPrice { get; set; }

    [Range(
        0,
        int.MaxValue,
        ErrorMessage = "Maksimumsprisen må ikke være negativ.")]
    public int? MaxPrice { get; set; }

    [Range(
        1886,
        2100,
        ErrorMessage = "Minimumsårgangen er ugyldig.")]
    public int? MinProductionYear { get; set; }

    [Range(
        0,
        int.MaxValue,
        ErrorMessage = "Kilometerstanden må ikke være negativ.")]
    public int? MaxMileage { get; set; }

    public string Sort { get; set; } = "newest";

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 12;
}