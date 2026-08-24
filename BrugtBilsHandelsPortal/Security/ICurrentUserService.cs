namespace BrugtBilsHandelsPortal.Security;

public interface ICurrentUserService
{
    Task<int?> GetUserIdAsync();
    Task<bool> IsAuthenticatedAsync();
    Task<bool> IsInRoleAsync(string role);
}