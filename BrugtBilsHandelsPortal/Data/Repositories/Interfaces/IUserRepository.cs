using BrugtBilsHandelsPortal.Models.Entities;

namespace BrugtBilsHandelsPortal.Data.Repositories.Interfaces;

/// <summary>Definerer persistensoperationer for brugeridentiteter og profiler.</summary>
public interface IUserRepository
{
    Task<bool> TestConnectionAsync();

    Task<User?> GetByEmailOrUsernameAsync(
        string emailOrUsername);

    Task<User?> GetByIdAsync(int userId);

    Task<bool> RegistrationIdentityExistsAsync(
        string username, string email);

    Task<int> CreateAsync(User user);
    
    Task<bool> IdentityExistsForOtherUserAsync(
        int userId,
        string username,
        string email);

    Task<bool> UpdateProfileAsync(User user);

    Task<IReadOnlyList<User>> GetAllForAdministrationAsync(string? search);
    Task<bool> SetActiveAsync(int userId, bool isActive);
    Task<bool> SetRoleAsync(int userId, string roleName);
    Task<bool> IsAdministratorAsync(int userId);
    Task AddAuditLogAsync(int? userId, string action, int targetUserId, string details);
}
