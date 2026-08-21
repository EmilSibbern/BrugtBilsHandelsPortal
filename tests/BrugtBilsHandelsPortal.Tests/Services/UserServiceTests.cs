using BrugtBilsHandelsPortal.DTOs.Users;
using BrugtBilsHandelsPortal.Models.Entities;
using BrugtBilsHandelsPortal.Services.Implementations;
using BrugtBilsHandelsPortal.Tests.Fakes;
using Xunit;

namespace BrugtBilsHandelsPortal.Tests.Services;

public class UserServiceTests
{
    [Fact]
    public async Task GetCurrentProfileAsync_WithAuthenticatedUser_ReturnsOwnProfile()
    {
        var repository = new FakeUserRepository();
        repository.Users.Add(CreateUser(id: 7));
        repository.Users.Add(CreateUser(id: 8, username: "anden"));

        var service = new UserService(
            repository,
            new FakeCurrentUserService { UserId = 7 });

        var profile = await service.GetCurrentProfileAsync();

        Assert.NotNull(profile);
        Assert.Equal(7, profile.Id);
        Assert.Equal("bruger7", profile.Username);
    }

    [Fact]
    public async Task GetCurrentProfileAsync_WithoutAuthentication_ReturnsNull()
    {
        var service = new UserService(
            new FakeUserRepository(),
            new FakeCurrentUserService { UserId = null });

        var profile = await service.GetCurrentProfileAsync();

        Assert.Null(profile);
    }

    [Fact]
    public async Task UpdateCurrentProfileAsync_UpdatesOnlyCurrentUser()
    {
        var repository = new FakeUserRepository();
        repository.Users.Add(CreateUser(id: 7));
        repository.Users.Add(CreateUser(id: 8, username: "anden"));

        var service = new UserService(
            repository,
            new FakeCurrentUserService { UserId = 7 });

        var result = await service.UpdateCurrentProfileAsync(
            new UpdateProfileDto
            {
                Username = "nyt-navn",
                Email = "nyt@example.com",
                Phone = "87654321",
                Address = "Ny vej 2"
            });

        Assert.True(result.Succeeded);

        var updated = repository.Users.Single(user => user.Id == 7);
        var untouched = repository.Users.Single(user => user.Id == 8);

        Assert.Equal("nyt-navn", updated.Username);
        Assert.Equal("nyt@example.com", updated.Email);
        Assert.Equal("anden", untouched.Username);
    }

    [Fact]
    public async Task UpdateCurrentProfileAsync_WithDuplicateIdentity_IsRejected()
    {
        var repository = new FakeUserRepository();
        repository.Users.Add(CreateUser(id: 7));
        repository.Users.Add(
            CreateUser(
                id: 8,
                username: "optaget",
                email: "optaget@example.com"));

        var service = new UserService(
            repository,
            new FakeCurrentUserService { UserId = 7 });

        var result = await service.UpdateCurrentProfileAsync(
            new UpdateProfileDto
            {
                Username = "optaget",
                Email = "ny@example.com"
            });

        Assert.False(result.Succeeded);
        Assert.Contains("allerede i brug", result.ErrorMessage);
    }

    [Fact]
    public async Task UpdateCurrentProfileAsync_WithoutAuthentication_IsRejected()
    {
        var service = new UserService(
            new FakeUserRepository(),
            new FakeCurrentUserService { UserId = null });

        var result = await service.UpdateCurrentProfileAsync(
            new UpdateProfileDto
            {
                Username = "bruger",
                Email = "bruger@example.com"
            });

        Assert.False(result.Succeeded);
        Assert.Contains("logget ind", result.ErrorMessage);
    }

    private static User CreateUser(
        int id,
        string? username = null,
        string? email = null)
    {
        return new User
        {
            Id = id,
            Username = username ?? $"bruger{id}",
            Email = email ?? $"bruger{id}@example.com",
            UserGroupId = 1,
            UserGroupName = "User",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }
}
