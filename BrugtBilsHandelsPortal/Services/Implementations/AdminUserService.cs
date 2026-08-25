using BrugtBilsHandelsPortal.Data.Repositories.Interfaces;
using BrugtBilsHandelsPortal.DTOs.Users;
using BrugtBilsHandelsPortal.Security;
using BrugtBilsHandelsPortal.Services.Interfaces;

namespace BrugtBilsHandelsPortal.Services.Implementations;

public sealed class AdminUserService(IUserRepository userRepository, ICurrentUserService currentUserService) : IAdminUserService
{
    public async Task<IReadOnlyList<AdminUserDto>> GetUsersAsync(string? search)
    {
        await EnsureAdministratorAsync();
        var users = await userRepository.GetAllForAdministrationAsync(search);
        return users.Select(user => new AdminUserDto { Id = user.Id, Username = user.Username, Email = user.Email, Role = user.UserGroupName, IsActive = user.IsActive, CreatedAt = user.CreatedAt }).ToList();
    }

    public async Task<bool> SetActiveAsync(int userId, bool isActive)
    {
        var adminId = await EnsureAdministratorAsync();
        var changed = await userRepository.SetActiveAsync(userId, isActive);
        if (changed) await userRepository.AddAuditLogAsync(adminId, isActive ? "UserActivated" : "UserDeactivated", userId, $"IsActive={isActive}");
        return changed;
    }

    public async Task<bool> SetRoleAsync(int userId, string roleName)
    {
        var adminId = await EnsureAdministratorAsync();
        if (roleName is not ("User" or "Administrator")) throw new ArgumentException("Ugyldig brugerrolle.", nameof(roleName));
        var changed = await userRepository.SetRoleAsync(userId, roleName);
        if (changed) await userRepository.AddAuditLogAsync(adminId, "UserRoleChanged", userId, $"Role={roleName}");
        return changed;
    }

    private async Task<int> EnsureAdministratorAsync()
    {
        var userId = await currentUserService.GetUserIdAsync();
        if (userId is null || !await userRepository.IsAdministratorAsync(userId.Value)) throw new UnauthorizedAccessException("Kun administratorer må administrere brugere.");
        return userId.Value;
    }
}