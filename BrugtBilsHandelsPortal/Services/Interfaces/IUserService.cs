using BrugtBilsHandelsPortal.DTOs.Users;

namespace BrugtBilsHandelsPortal.Services.Interfaces;

/// <summary>Tilbyder profilfunktioner afgrænset til den aktuelle bruger.</summary>
public interface IUserService
{
    /// <summary>Henter profilen for den bruger, der er logget ind.</summary>
    Task<UserProfileDto?> GetCurrentProfileAsync();
    
    /// <summary>Validerer og opdaterer den aktuelle brugers profil.</summary>
    Task<ProfileUpdateResult> UpdateCurrentProfileAsync(
        UpdateProfileDto request);
}
