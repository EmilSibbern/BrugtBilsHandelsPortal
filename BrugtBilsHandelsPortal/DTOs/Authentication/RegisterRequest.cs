using System.ComponentModel.DataAnnotations;

namespace BrugtBilsHandelsPortal.DTOs.Authentication;

public class RegisterRequest
{
    [Required(ErrorMessage = "Brugernavn er påkrævet.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Brugernavn skal være mellem 3 og 50 tegn.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-mail er påkrævet.")]
    [EmailAddress(ErrorMessage = "Skriv en gyldig e-mailadresse.")]
    public string Email { get; set; } = string.Empty;

    [StringLength(30)]
    public string? Phone { get; set; }

    [StringLength(250)]
    public string? Address { get; set; }

    [Required(ErrorMessage = "Password er påkrævet.")]
    [StringLength(200, MinimumLength = 8, ErrorMessage = "Password skal være mindst 8 tegn.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Gentag dit password.")]
    [Compare(nameof(Password), ErrorMessage = "De to passwords er ikke ens.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}