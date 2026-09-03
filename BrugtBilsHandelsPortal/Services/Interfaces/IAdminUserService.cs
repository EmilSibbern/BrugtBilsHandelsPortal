using BrugtBilsHandelsPortal.DTOs.Users;

namespace BrugtBilsHandelsPortal.Services.Interfaces;

/// <summary>
/// Håndhæver validering og administratoradgang
/// til brugeradministration.
/// </summary>
public interface IAdminUserService
{
    /// <summary>
    /// Validerer søgekriterierne og returnerer en
    /// side med ikke-følsomme brugeroplysninger.
    /// </summary>
    Task<AdminUserSearchResult> SearchAsync(AdminUserSearchDto search);

    /// <summary>
    /// Deaktiverer en bruger, hvis den aktuelle bruger
    /// er en aktiv administrator, og sikkerhedsreglerne
    /// tillader handlingen.
    /// </summary>
    Task DeactivateAsync(int targetUserId);

    /// <summary>
    /// Genaktiverer en deaktiveret bruger, hvis den aktuelle
    /// bruger er en aktiv administrator.
    /// </summary>
    Task ReactivateAsync(int targetUserId);
}