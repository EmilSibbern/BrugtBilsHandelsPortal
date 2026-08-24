using BrugtBilsHandelsPortal.Data.Repositories.Interfaces;
using BrugtBilsHandelsPortal.DTOs.Users;
using BrugtBilsHandelsPortal.Security;
using BrugtBilsHandelsPortal.Services.Interfaces;
using System.ComponentModel.DataAnnotations;
using BrugtBilsHandelsPortal.Models.Entities;
namespace BrugtBilsHandelsPortal.Services.Implementations;

/// <summary>Afgrænser læsning og redigering af profiler til den aktuelle bruger.</summary>
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;

    public UserService(
        IUserRepository userRepository,
        ICurrentUserService currentUserService)
    {
        _userRepository = userRepository;
        _currentUserService = currentUserService;
    }

    public async Task<UserProfileDto?> GetCurrentProfileAsync()
    {
        var userId = await _currentUserService.GetUserIdAsync();

        if (userId is null)
        {
            return null;
        }

        var user = await _userRepository.GetByIdAsync(userId.Value);

        if (user is null)
        {
            return null;
        }

        return new UserProfileDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Phone = user.Phone,
            Address = user.Address,
            UserGroupName = user.UserGroupName,
            CreatedAt = user.CreatedAt
        };
    }
    
    public async Task<ProfileUpdateResult> UpdateCurrentProfileAsync(
        UpdateProfileDto request)
    {
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            validationResults,
            validateAllProperties: true);

        if (!isValid)
        {
            var errorMessage =
                validationResults.FirstOrDefault()?.ErrorMessage
                ?? "Profiloplysningerne er ugyldige.";

            return ProfileUpdateResult.Failed(errorMessage);
        }

        var userId = await _currentUserService.GetUserIdAsync();

        if (userId is null)
        {
            return ProfileUpdateResult.Failed(
                "Du skal være logget ind for at redigere profilen.");
        }

        var username = request.Username.Trim();

        var email = request.Email
            .Trim()
            .ToLowerInvariant();

        var identityExists =
            await _userRepository.IdentityExistsForOtherUserAsync(
                userId.Value,
                username,
                email);

        if (identityExists)
        {
            return ProfileUpdateResult.Failed(
                "Brugernavn eller e-mail er allerede i brug.");
        }

        var user = new User
        {
            Id = userId.Value,
            Username = username,
            Email = email,

            Phone = string.IsNullOrWhiteSpace(request.Phone)
                ? null
                : request.Phone.Trim(),

            Address = string.IsNullOrWhiteSpace(request.Address)
                ? null
                : request.Address.Trim()
        };

        var updated =
            await _userRepository.UpdateProfileAsync(user);

        if (!updated)
        {
            return ProfileUpdateResult.Failed(
                "Profilen blev ikke fundet eller kunne ikke opdateres.");
        }

        return ProfileUpdateResult.Successful();
    }
}
