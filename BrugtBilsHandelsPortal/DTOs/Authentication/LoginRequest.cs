using System.ComponentModel.DataAnnotations;

namespace BrugtBilsHandelsPortal.DTOs.Authentication;

public class LoginRequest
{
    [Required(ErrorMessage = "Skriv brugernavn eller email")]
    public string EmailOrUsername { get; set; } = string.Empty;

    [Required(ErrorMessage = "Skriv dit password")]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}