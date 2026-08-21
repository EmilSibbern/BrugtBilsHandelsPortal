using BrugtBilsHandelsPortal.Models.Entities;

namespace BrugtBilsHandelsPortal.Data.Repositories.Interfaces;

public interface IUserRepository
{
    Task<bool> TestConnectionAsync();

    Task<User?> GetByEmailOrUsernameAsync(
        string emailOrUsername);

    Task<User?> GetByIdAsync(int userId);

    Task<bool> RegistrationIdentityExistsAsync(
        string username, string email);

    Task<int> CreateAsync(User user);
    
    Task<bool> IdentityExistsForOtherUserAsync(
        int userId,
        string username,
        string email);

    Task<bool> UpdateProfileAsync(User user);
}