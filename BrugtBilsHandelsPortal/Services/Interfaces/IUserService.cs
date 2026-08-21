using BrugtBilsHandelsPortal.DTOs.Users;

namespace BrugtBilsHandelsPortal.Services.Interfaces;

public interface IUserService
{
    Task<UserProfileDto?> GetCurrentProfileAsync();
    
    Task<ProfileUpdateResult> UpdateCurrentProfileAsync(
        UpdateProfileDto request);
}