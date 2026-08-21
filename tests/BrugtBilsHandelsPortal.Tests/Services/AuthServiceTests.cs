using BrugtBilsHandelsPortal.DTOs.Authentication;
using BrugtBilsHandelsPortal.Models.Entities;
using BrugtBilsHandelsPortal.Services.Implementations;
using BrugtBilsHandelsPortal.Tests.Fakes;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace BrugtBilsHandelsPortal.Tests.Services;

public class AuthServiceTests
{
    private readonly FakeUserRepository _repository = new();
    private readonly PasswordHasher<User> _passwordHasher = new();

    [Fact]
    public async Task RegisterAsync_WithValidRequest_CreatesUserWithHashedPassword()
    {
        var service = CreateService();
        var request = ValidRegistration();

        var result = await service.RegisterAsync(request);

        Assert.True(result.Succeeded);
        Assert.NotNull(_repository.CreatedUser);
        Assert.Equal("User", _repository.CreatedUser.UserGroupName);
        Assert.NotEqual(request.Password, _repository.CreatedUser.PasswordHash);

        var verification = _passwordHasher.VerifyHashedPassword(
            _repository.CreatedUser,
            _repository.CreatedUser.PasswordHash,
            request.Password);

        Assert.NotEqual(PasswordVerificationResult.Failed, verification);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_DoesNotCreateUser()
    {
        _repository.Users.Add(CreateUser(email: "emil@example.com"));
        var service = CreateService();
        var request = ValidRegistration();
        request.Email = "emil@example.com";

        var result = await service.RegisterAsync(request);

        Assert.False(result.Succeeded);
        Assert.Null(_repository.CreatedUser);
        Assert.Contains("allerede i brug", result.ErrorMessage);
    }

    [Fact]
    public async Task RegisterAsync_WithMismatchedPasswords_DoesNotCreateUser()
    {
        var service = CreateService();
        var request = ValidRegistration();
        request.ConfirmPassword = "EtAndetPassword123!";

        var result = await service.RegisterAsync(request);

        Assert.False(result.Succeeded);
        Assert.Null(_repository.CreatedUser);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsIdentityAndRole()
    {
        var user = CreateUser();
        user.PasswordHash = _passwordHasher.HashPassword(user, "Password123!");
        _repository.Users.Add(user);
        var service = CreateService();

        var result = await service.LoginAsync(new LoginRequest
        {
            EmailOrUsername = user.Email,
            Password = "Password123!"
        });

        Assert.True(result.Succeeded);
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal(user.Username, result.Username);
        Assert.Equal("User", result.Role);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ReturnsGenericError()
    {
        var user = CreateUser();
        user.PasswordHash = _passwordHasher.HashPassword(user, "Password123!");
        _repository.Users.Add(user);
        var service = CreateService();

        var result = await service.LoginAsync(new LoginRequest
        {
            EmailOrUsername = user.Username,
            Password = "ForkertPassword"
        });

        Assert.False(result.Succeeded);
        Assert.Equal(
            "Brugernavn/e-mail eller password er forkert.",
            result.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_WithInactiveUser_IsRejected()
    {
        var user = CreateUser();
        user.IsActive = false;
        user.PasswordHash = _passwordHasher.HashPassword(user, "Password123!");
        _repository.Users.Add(user);
        var service = CreateService();

        var result = await service.LoginAsync(new LoginRequest
        {
            EmailOrUsername = user.Username,
            Password = "Password123!"
        });

        Assert.False(result.Succeeded);
        Assert.Equal("Brugerkontoen er deaktiveret.", result.ErrorMessage);
    }

    private AuthService CreateService()
    {
        return new AuthService(_repository, _passwordHasher);
    }

    private static RegisterRequest ValidRegistration()
    {
        return new RegisterRequest
        {
            Username = "nybruger",
            Email = "ny@example.com",
            Phone = "12345678",
            Address = "Testvej 1",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };
    }

    private static User CreateUser(
        string email = "bruger@example.com")
    {
        return new User
        {
            Id = 1,
            Username = "bruger",
            Email = email,
            UserGroupId = 1,
            UserGroupName = "User",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }
}
