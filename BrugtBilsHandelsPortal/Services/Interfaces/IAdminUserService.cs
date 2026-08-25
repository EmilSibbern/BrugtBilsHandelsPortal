using BrugtBilsHandelsPortal.DTOs.Users;

namespace BrugtBilsHandelsPortal.Services.Interfaces;

public interface IAdminUserService
{
    Task<IReadOnlyList<AdminUserDto>> GetUsersAsync(string? search);
    Task<bool> SetActiveAsync(int userId, bool isActive);
    Task<bool> SetRoleAsync(int userId, string roleName);
}