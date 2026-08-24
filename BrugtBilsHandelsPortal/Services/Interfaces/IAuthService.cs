using BrugtBilsHandelsPortal.DTOs.Authentication;

namespace BrugtBilsHandelsPortal.Services.Interfaces;

/// <summary>Håndterer validering af login og registrering uafhængigt af HTTP-laget.</summary>
public interface IAuthService
{
    /// <summary>Kontrollerer legitimationsoplysninger og returnerer de claims-relevante brugerdata.</summary>
    Task<AuthenticationResult> LoginAsync(
        LoginRequest request);

    /// <summary>Validerer og opretter en ny bruger med en sikkert hashet adgangskode.</summary>
    Task<AuthenticationResult> RegisterAsync(
        RegisterRequest request);
}
