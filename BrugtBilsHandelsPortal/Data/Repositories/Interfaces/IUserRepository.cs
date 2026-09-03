using BrugtBilsHandelsPortal.DTOs.Common;
using BrugtBilsHandelsPortal.DTOs.Users;
using BrugtBilsHandelsPortal.Models.Entities;

namespace BrugtBilsHandelsPortal.Data.Repositories.Interfaces;

/// <summary>Definerer persistensoperationer for brugeridentiteter og profiler.</summary>
public interface IUserRepository
{
    Task<bool> TestConnectionAsync();

    Task<User?> GetByEmailOrUsernameAsync(string emailOrUsername);

    Task<User?> GetByIdAsync(int userId);

    Task<bool> RegistrationIdentityExistsAsync(string username, string email);

    Task<int> CreateAsync(User user);

    Task<bool> IdentityExistsForOtherUserAsync(int userId, string username, string email);

    Task<bool> UpdateProfileAsync(User user);

    /// <summary>
    /// Søger i ikke-følsomme kontooplysninger til
    /// det beskyttede adminområde.
    /// </summary>
    Task<PagedResult<AdminUserDto>> SearchForAdministrationAsync(AdminUserSearchDto search);

    /// <summary>
    /// Deaktiverer en bruger og skriver auditloggen
    /// i samme databasetransaktion.
    /// </summary>
    Task<AdminUserDeactivationStatus> DeactivateWithAuditAsync(int targetUserId, int administratorUserId, Guid correlationId);

    /// <summary>
    /// Genaktiverer en bruger og skriver auditloggen
    /// i samme databasetransaktion.
    /// </summary>
    Task<AdminUserActivationStatus> ReactivateWithAuditAsync(int targetUserId, int administratorUserId, Guid correlationId);
}