using BrugtBilsHandelsPortal.Security;

namespace BrugtBilsHandelsPortal.Tests.Fakes;

public class FakeCurrentUserService : ICurrentUserService
{
    public int? UserId { get; set; }

    public Task<int?> GetUserIdAsync()
    {
        return Task.FromResult(UserId);
    }

    public Task<bool> IsAuthenticatedAsync()
    {
        return Task.FromResult(UserId.HasValue);
    }

    public bool IsAdministrator { get; set; }

    public Task<bool> IsInRoleAsync(string role)
    {
        return Task.FromResult(role == "Administrator" && IsAdministrator);
    }
}