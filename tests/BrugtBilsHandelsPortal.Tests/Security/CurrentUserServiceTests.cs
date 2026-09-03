using System.Security.Claims;
using BrugtBilsHandelsPortal.Models.Entities;
using BrugtBilsHandelsPortal.Security;
using BrugtBilsHandelsPortal.Tests.Fakes;
using Microsoft.AspNetCore.Components.Authorization;
using Xunit;

namespace BrugtBilsHandelsPortal.Tests.Security;

public class CurrentUserServiceTests
{
    [Fact]
    [Trait("UserStory", "US16")]
    public async Task ActiveUser_IsAuthenticatedAndReturnsDatabaseRole()
    {
        var repository = new FakeUserRepository();
        repository.Users.Add(CreateUser(id: 7, role: "Administrator", isActive: true));
        var service = CreateService(repository, CreatePrincipal(userId: "7", cookieRole: "Administrator"));

        var userId = await service.GetUserIdAsync();
        var isAuthenticated = await service.IsAuthenticatedAsync();
        var isAdministrator = await service.IsInRoleAsync("Administrator");

        Assert.Equal(7, userId);
        Assert.True(isAuthenticated);
        Assert.True(isAdministrator);
    }

    [Fact]
    [Trait("UserStory", "US16")]
    public async Task DeactivatedUser_IsTreatedAsAnonymous()
    {
        var repository = new FakeUserRepository();
        repository.Users.Add(CreateUser(id: 7, role: "User", isActive: false));
        var service = CreateService(repository, CreatePrincipal(userId: "7", cookieRole: "User"));

        Assert.Null(await service.GetUserIdAsync());
        Assert.False(await service.IsAuthenticatedAsync());
        Assert.False(await service.IsInRoleAsync("User"));
    }

    [Fact]
    [Trait("UserStory", "US16")]
    public async Task DatabaseRoleOverridesStaleCookieRole()
    {
        var repository = new FakeUserRepository();
        repository.Users.Add(CreateUser(id: 7, role: "User", isActive: true));
        var service = CreateService(repository, CreatePrincipal(userId: "7", cookieRole: "Administrator"));

        Assert.False(await service.IsInRoleAsync("Administrator"));
        Assert.True(await service.IsInRoleAsync("User"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-number")]
    [Trait("UserStory", "US16")]
    public async Task MissingOrInvalidUserIdClaim_IsRejected(string? userId)
    {
        var repository = new FakeUserRepository();
        repository.Users.Add(CreateUser(id: 7, role: "User", isActive: true));
        var service = CreateService(repository, CreatePrincipal(userId, cookieRole: "User"));

        Assert.Null(await service.GetUserIdAsync());
        Assert.False(await service.IsAuthenticatedAsync());
        Assert.False(await service.IsInRoleAsync("User"));
    }

    [Fact]
    [Trait("UserStory", "US16")]
    public async Task AnonymousPrincipal_IsRejected()
    {
        var repository = new FakeUserRepository();
        var service = CreateService(repository, new ClaimsPrincipal(new ClaimsIdentity()));

        Assert.Null(await service.GetUserIdAsync());
        Assert.False(await service.IsAuthenticatedAsync());
        Assert.False(await service.IsInRoleAsync("Administrator"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [Trait("UserStory", "US16")]
    public async Task EmptyRole_IsRejected(string role)
    {
        var repository = new FakeUserRepository();
        repository.Users.Add(CreateUser(id: 7, role: "User", isActive: true));
        var service = CreateService(repository, CreatePrincipal(userId: "7", cookieRole: "User"));

        Assert.False(await service.IsInRoleAsync(role));
    }

    private static CurrentUserService CreateService(FakeUserRepository repository, ClaimsPrincipal principal)
    {
        return new CurrentUserService(new StubAuthenticationStateProvider(principal), repository);
    }

    private static ClaimsPrincipal CreatePrincipal(string? userId, string cookieRole)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "test-user"),
            new(ClaimTypes.Role, cookieRole)
        };

        if (userId is not null)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "TestCookie"));
    }

    private static User CreateUser(int id, string role, bool isActive)
    {
        return new User
        {
            Id = id,
            Username = "test-user",
            Email = "test@example.dk",
            UserGroupName = role,
            IsActive = isActive,
            CreatedAt = new DateTime(2026, 8, 31)
        };
    }

    private sealed class StubAuthenticationStateProvider(ClaimsPrincipal principal) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            return Task.FromResult(new AuthenticationState(principal));
        }
    }
}