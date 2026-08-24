using System.ComponentModel.DataAnnotations;
using BrugtBilsHandelsPortal.Data.Repositories.Interfaces;
using BrugtBilsHandelsPortal.DTOs.Authentication;
using BrugtBilsHandelsPortal.Models.Entities;
using BrugtBilsHandelsPortal.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace BrugtBilsHandelsPortal.Services.Implementations;

/// <summary>Validerer legitimationsoplysninger og opretter sikre brugeridentiteter.</summary>
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher<User> _passwordHasher;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher<User> passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthenticationResult> LoginAsync(
        LoginRequest request)
    {
        var login = request.EmailOrUsername.Trim();

        if (string.IsNullOrWhiteSpace(login) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return AuthenticationResult.Failed(
                "Brugernavn/e-mail og password skal udfyldes.");
        }

        var user =
            await _userRepository
                .GetByEmailOrUsernameAsync(login);

        if (user is null)
        {
            return AuthenticationResult.Failed(
                "Brugernavn/e-mail eller password er forkert.");
        }

        var passwordResult =
            _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                request.Password);

        if (passwordResult ==
            PasswordVerificationResult.Failed)
        {
            return AuthenticationResult.Failed(
                "Brugernavn/e-mail eller password er forkert.");
        }

        if (!user.IsActive)
        {
            return AuthenticationResult.Failed(
                "Brugerkontoen er deaktiveret.");
        }

        return AuthenticationResult.Successful(
            user.Id,
            user.Username,
            user.Email,
            user.UserGroupName);
    }
    public async Task<AuthenticationResult> RegisterAsync(
        RegisterRequest request)
    {
        var validationResults =
            new List<ValidationResult>();

        var validationContext =
            new ValidationContext(request);

        var isValid = Validator.TryValidateObject(
            request,
            validationContext,
            validationResults,
            validateAllProperties: true);

        if (!isValid)
        {
            var errorMessage =
                validationResults.FirstOrDefault()?.ErrorMessage
                ?? "Registreringsoplysningerne er ugyldige.";

            return AuthenticationResult.Failed(errorMessage);
        }

        var username = request.Username.Trim();

        var email = request.Email
            .Trim()
            .ToLowerInvariant();

        var identityExists =
            await _userRepository
                .RegistrationIdentityExistsAsync(
                    username,
                    email);

        if (identityExists)
        {
            return AuthenticationResult.Failed(
                "Brugernavn eller e-mail er allerede i brug.");
        }

        var user = new User
        {
            Username = username,
            Email = email,

            Phone = string.IsNullOrWhiteSpace(request.Phone)
                ? null
                : request.Phone.Trim(),

            Address = string.IsNullOrWhiteSpace(request.Address)
                ? null
                : request.Address.Trim(),

            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UserGroupName = "User"
        };

        user.PasswordHash =
            _passwordHasher.HashPassword(
                user,
                request.Password);

        var userId =
            await _userRepository.CreateAsync(user);

        return AuthenticationResult.Successful(
            userId,
            user.Username,
            user.Email,
            "User");
    }
}
