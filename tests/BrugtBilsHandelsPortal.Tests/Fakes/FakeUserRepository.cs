using BrugtBilsHandelsPortal.Data.Repositories.Interfaces;
using BrugtBilsHandelsPortal.DTOs.Common;
using BrugtBilsHandelsPortal.DTOs.Users;
using BrugtBilsHandelsPortal.Models.Entities;

namespace BrugtBilsHandelsPortal.Tests.Fakes;

public class FakeUserRepository : IUserRepository
{
    public List<User> Users { get; } = [];

    public User? CreatedUser { get; private set; }
    public int SearchForAdministrationCallCount { get; private set; }
    public int DeactivateWithAuditCallCount { get; private set; }
    public int ReactivateWithAuditCallCount { get; private set; }
    public AdminUserSearchDto? LastAdminSearch { get; private set; }
    public int? LastTargetUserId { get; private set; }
    public int? LastAdministratorUserId { get; private set; }
    public Guid? LastCorrelationId { get; private set; }
    public int? LastReactivatedUserId { get; private set; }
    public int? LastReactivationAdministratorUserId { get; private set; }
    public Guid? LastReactivationCorrelationId { get; private set; }

    public AdminUserDeactivationStatus DeactivationStatus { get; set; } = AdminUserDeactivationStatus.Succeeded;

    public AdminUserActivationStatus ActivationStatus { get; set; } = AdminUserActivationStatus.Succeeded;

    public Task<bool> TestConnectionAsync()
    {
        return Task.FromResult(true);
    }

    public Task<User?> GetByEmailOrUsernameAsync(string emailOrUsername)
    {
        var user = Users.FirstOrDefault(candidate => string.Equals(candidate.Email, emailOrUsername, StringComparison.OrdinalIgnoreCase) || string.Equals(candidate.Username, emailOrUsername, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(user);
    }

    public Task<User?> GetByIdAsync(int userId)
    {
        var user = Users.FirstOrDefault(candidate => candidate.Id == userId && candidate.IsActive);

        return Task.FromResult(user);
    }

    public Task<bool> RegistrationIdentityExistsAsync(string username, string email)
    {
        var exists = Users.Any(candidate => string.Equals(candidate.Username, username, StringComparison.OrdinalIgnoreCase) || string.Equals(candidate.Email, email, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(exists);
    }

    public Task<int> CreateAsync(User user)
    {
        user.Id = Users.Count == 0 ? 1 : Users.Max(candidate => candidate.Id) + 1;

        user.UserGroupId = 1;
        user.UserGroupName = "User";

        Users.Add(user);
        CreatedUser = user;

        return Task.FromResult(user.Id);
    }

    public Task<bool> IdentityExistsForOtherUserAsync(int userId, string username, string email)
    {
        var exists = Users.Any(candidate => candidate.Id != userId && (string.Equals(candidate.Username, username, StringComparison.OrdinalIgnoreCase) || string.Equals(candidate.Email, email, StringComparison.OrdinalIgnoreCase)));

        return Task.FromResult(exists);
    }

    public Task<bool> UpdateProfileAsync(User user)
    {
        var existingUser = Users.FirstOrDefault(candidate => candidate.Id == user.Id && candidate.IsActive);

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

    public Task<PagedResult<AdminUserDto>> SearchForAdministrationAsync(AdminUserSearchDto search)
    {
        SearchForAdministrationCallCount++;
        LastAdminSearch = search;

        IEnumerable<User> query = Users;

        if (!string.IsNullOrWhiteSpace(search.Query))
        {
            query = query.Where(user => user.Username.Contains(search.Query, StringComparison.OrdinalIgnoreCase) || user.Email.Contains(search.Query, StringComparison.OrdinalIgnoreCase));
        }

        if (search.IsActive.HasValue)
        {
            query = query.Where(user => user.IsActive == search.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search.Role))
        {
            query = query.Where(user => string.Equals(user.UserGroupName, search.Role, StringComparison.Ordinal));
        }

        var matchingUsers = query.ToList();
        var users = matchingUsers.Skip((search.Page - 1) * search.PageSize).Take(search.PageSize).Select(user => new AdminUserDto { Id = user.Id, Username = user.Username, Email = user.Email, Role = user.UserGroupName, IsActive = user.IsActive, CreatedAt = user.CreatedAt }).ToArray();

        return Task.FromResult(new PagedResult<AdminUserDto> { Items = users, Page = search.Page, PageSize = search.PageSize, TotalItems = matchingUsers.Count });
    }

    public Task<AdminUserDeactivationStatus> DeactivateWithAuditAsync(int targetUserId, int administratorUserId, Guid correlationId)
    {
        DeactivateWithAuditCallCount++;
        LastTargetUserId = targetUserId;
        LastAdministratorUserId = administratorUserId;
        LastCorrelationId = correlationId;

        return Task.FromResult(DeactivationStatus);
    }

    public Task<AdminUserActivationStatus> ReactivateWithAuditAsync(int targetUserId, int administratorUserId, Guid correlationId)
    {
        ReactivateWithAuditCallCount++;
        LastReactivatedUserId = targetUserId;
        LastReactivationAdministratorUserId = administratorUserId;
        LastReactivationCorrelationId = correlationId;

        return Task.FromResult(ActivationStatus);
    }
}