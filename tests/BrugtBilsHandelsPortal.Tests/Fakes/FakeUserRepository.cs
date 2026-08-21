using BrugtBilsHandelsPortal.Data.Repositories.Interfaces;
using BrugtBilsHandelsPortal.Models.Entities;

namespace BrugtBilsHandelsPortal.Tests.Fakes;

public class FakeUserRepository : IUserRepository
{
    public List<User> Users { get; } = [];

    public User? CreatedUser { get; private set; }

    public Task<bool> TestConnectionAsync()
    {
        return Task.FromResult(true);
    }

    public Task<User?> GetByEmailOrUsernameAsync(
        string emailOrUsername)
    {
        var user = Users.FirstOrDefault(
            candidate =>
                string.Equals(
                    candidate.Email,
                    emailOrUsername,
                    StringComparison.OrdinalIgnoreCase)
                ||
                string.Equals(
                    candidate.Username,
                    emailOrUsername,
                    StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(user);
    }

    public Task<User?> GetByIdAsync(int userId)
    {
        var user = Users.FirstOrDefault(
            candidate =>
                candidate.Id == userId
                && candidate.IsActive);

        return Task.FromResult(user);
    }

    public Task<bool> RegistrationIdentityExistsAsync(
        string username,
        string email)
    {
        var exists = Users.Any(
            candidate =>
                string.Equals(
                    candidate.Username,
                    username,
                    StringComparison.OrdinalIgnoreCase)
                ||
                string.Equals(
                    candidate.Email,
                    email,
                    StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(exists);
    }

    public Task<int> CreateAsync(User user)
    {
        user.Id = Users.Count == 0
            ? 1
            : Users.Max(candidate => candidate.Id) + 1;

        user.UserGroupId = 1;
        user.UserGroupName = "User";

        Users.Add(user);
        CreatedUser = user;

        return Task.FromResult(user.Id);
    }

    public Task<bool> IdentityExistsForOtherUserAsync(
        int userId,
        string username,
        string email)
    {
        var exists = Users.Any(
            candidate =>
                candidate.Id != userId
                &&
                (string.Equals(
                     candidate.Username,
                     username,
                     StringComparison.OrdinalIgnoreCase)
                 ||
                 string.Equals(
                     candidate.Email,
                     email,
                     StringComparison.OrdinalIgnoreCase)));

        return Task.FromResult(exists);
    }

    public Task<bool> UpdateProfileAsync(User user)
    {
        var existingUser = Users.FirstOrDefault(
            candidate =>
                candidate.Id == user.Id
                && candidate.IsActive);

        if (existingUser is null)
        {
            return Task.FromResult(false);
        }

        existingUser.Username = user.Username;
        existingUser.Email = user.Email;
        existingUser.Phone = user.Phone;
        existingUser.Address = user.Address;

        return Task.FromResult(true);
    }
}
