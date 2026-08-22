using System.ComponentModel.DataAnnotations;

namespace BrugtBilsHandelsPortal.DTOs.Announcements;

public class CreateAnnouncementDto
{
    [Required(ErrorMessage = "Du skal vælge et bilmærke.")]
    [Range(1, int.MaxValue, ErrorMessage = "Du skal vælge et bilmærke.")]
    public int BrandId { get; set; }

    [Required(ErrorMessage = "Model skal udfyldes.")]
    [StringLength(
        100,
        ErrorMessage = "Model må højst være 100 tegn.")]
    public string Model { get; set; } = string.Empty;

    [Required(ErrorMessage = "Produktionsår skal udfyldes.")]
    [Range(
        1886,
        2100,
        ErrorMessage = "Produktionsåret er ugyldigt.")]
    public int ProductionYear { get; set; }

    [Required(ErrorMessage = "Kilometerstand skal udfyldes.")]
    [Range(
        0,
        int.MaxValue,
        ErrorMessage = "Kilometerstanden må ikke være negativ.")]
    public int Mileage { get; set; }

    [StringLength(
        50,
        ErrorMessage = "Farve må højst være 50 tegn.")]
    public string? Color { get; set; }

    [Required(ErrorMessage = "Titel skal udfyldes.")]
    [StringLength(
        150,
        MinimumLength = 3,
        ErrorMessage = "Titel skal være mellem 3 og 150 tegn.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Beskrivelse skal udfyldes.")]
    public string Description { get; set; } = string.Empty;

    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "Prisen skal være mindst 1 kr.")]
    public int Price { get; set; }
}
