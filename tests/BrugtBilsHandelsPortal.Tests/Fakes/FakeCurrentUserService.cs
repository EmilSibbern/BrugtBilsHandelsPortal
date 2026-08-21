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
}
