using System.ComponentModel.DataAnnotations;

namespace BrugtBilsHandelsPortal.DTOs.Users;

/// <summary>
/// Kontrollerede søge- og pagingværdier til admin-brugerlisten.
/// </summary>
public class AdminUserSearchDto
{
    [StringLength(100, ErrorMessage = "Søgningen må højst være 100 tegn.")]
    public string? Query { get; set; }

    public bool? IsActive { get; set; }

    [RegularExpression("^(User|Administrator)$", ErrorMessage = "Den valgte rolle er ugyldig.")]
    public string? Role { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Siden er ugyldig.")]
    public int Page { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "Der kan højst vises 100 brugere ad gangen.")]
    public int PageSize { get; set; } = 20;
}