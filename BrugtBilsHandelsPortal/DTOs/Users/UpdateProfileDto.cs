using System.ComponentModel.DataAnnotations;

namespace BrugtBilsHandelsPortal.DTOs.Users;

public class UpdateProfileDto
{
    [Required(ErrorMessage = "Brugernavn er påkrævet.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Brugernavn skal være mellem 3 og 50 tegn.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-mail er påkrævet.")]
    [EmailAddress(ErrorMessage = "Skriv en gyldig e-mailadresse.")]
    [StringLength(254, ErrorMessage = "E-mail må højst være 254 tegn.")]
    public string Email { get; set; } = string.Empty;

    [StringLength(30, ErrorMessage = "Telefonnummer må højst være 30 tegn.")]
    public string? Phone { get; set; }

    [StringLength(250, ErrorMessage = "Adresse må højst være 250 tegn.")]
    public string? Address { get; set; }
}