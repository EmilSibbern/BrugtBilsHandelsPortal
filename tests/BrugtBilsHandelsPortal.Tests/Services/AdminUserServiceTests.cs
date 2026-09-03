using System.ComponentModel.DataAnnotations;
using BrugtBilsHandelsPortal.DTOs.Users;
using BrugtBilsHandelsPortal.Models.Entities;
using BrugtBilsHandelsPortal.Services.Implementations;
using BrugtBilsHandelsPortal.Tests.Fakes;
using Xunit;

namespace BrugtBilsHandelsPortal.Tests.Services;

public class AdminUserServiceTests
{
    [Fact]
    [Trait("UserStory", "US16")]
    public async Task SearchAsync_AsAdministrator_NormalizesAndReturnsUsers()
    {
        var repository = new FakeUserRepository();
        repository.Users.Add(new User { Id = 7, Username = "anna", Email = "anna@example.dk", UserGroupName = "User", IsActive = true, CreatedAt = new DateTime(2026, 8, 27) });
        var service = CreateService(repository, userId: 1, isAdministrator: true);
        var search = new AdminUserSearchDto
        {
            Query = "  anna  ",
            Role = "User"
        };

        var result = await service.SearchAsync(search);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Page);
        Assert.Single(result.Page.Items);
        Assert.Equal("anna", search.Query);
        Assert.Equal("User", search.Role);
        Assert.Equal(1, repository.SearchForAdministrationCallCount);
    }

    [Fact]
    [Trait("UserStory", "US16")]
    public async Task SearchAsync_WithInvalidRole_IsRejectedBeforeRepository()
    {
        var repository = new FakeUserRepository();
        var service = CreateService(repository, userId: 1, isAdministrator: true);

        var result = await service.SearchAsync(new AdminUserSearchDto { Role = "SuperAdministrator" });

        Assert.False(result.Succeeded);
        Assert.NotNull(result.ErrorMessage);
        Assert.Equal(0, repository.SearchForAdministrationCallCount);
    }

    [Fact]
    [Trait("UserStory", "US16")]
    public async Task SearchAsync_AsGuest_IsRejectedBeforeRepository()
    {
        var repository = new FakeUserRepository();
        var service = CreateService(repository, userId: null, isAdministrator: false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.SearchAsync(new AdminUserSearchDto()));

        Assert.Equal(0, repository.SearchForAdministrationCallCount);
    }

    [Fact]
    [Trait("UserStory", "US16")]
    public async Task SearchAsync_AsOrdinaryUser_IsRejectedBeforeRepository()
    {
        var repository = new FakeUserRepository();
        var service = CreateService(repository, userId: 9, isAdministrator: false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.SearchAsync(new AdminUserSearchDto()));

        Assert.Equal(0, repository.SearchForAdministrationCallCount);
    }

    [Fact]
    [Trait("UserStory", "US16")]
    public async Task DeactivateAsync_AsAdministrator_PassesActorAndCorrelationId()
    {
        var repository = new FakeUserRepository();
        var service = CreateService(repository, userId: 1, isAdministrator: true);

        await service.DeactivateAsync(42);

        Assert.Equal(1, repository.DeactivateWithAuditCallCount);
        Assert.Equal(42, repository.LastTargetUserId);
        Assert.Equal(1, repository.LastAdministratorUserId);
        Assert.NotNull(repository.LastCorrelationId);
        Assert.NotEqual(Guid.Empty, repository.LastCorrelationId);
    }

    [Fact]
    [Trait("UserStory", "US16")]
    public async Task DeactivateAsync_WithInvalidId_IsRejectedBeforeRepository()
    {
        var repository = new FakeUserRepository();
        var service = CreateService(repository, userId: 1, isAdministrator: true);

        await Assert.ThrowsAsync<ValidationException>(() => service.DeactivateAsync(0));

        Assert.Equal(0, repository.DeactivateWithAuditCallCount);
    }

    [Theory]
    [InlineData(AdminUserDeactivationStatus.UserNotFound, "findes ikke")]
    [InlineData(AdminUserDeactivationStatus.AlreadyInactive, "allerede deaktiveret")]
    [InlineData(AdminUserDeactivationStatus.CannotDeactivateSelf, "din egen konto")]
    [InlineData(AdminUserDeactivationStatus.LastActiveAdministrator, "sidste aktive administrator")]
    [Trait("UserStory", "US16")]
    public async Task DeactivateAsync_WhenBusinessRuleRejects_ShowsSafeError(AdminUserDeactivationStatus status, string expectedMessage)
    {
        var repository = new FakeUserRepository
        {
            DeactivationStatus = status
        };
        var service = CreateService(repository, userId: 1, isAdministrator: true);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeactivateAsync(42));

        Assert.Contains(expectedMessage, exception.Message);
        Assert.Equal(1, repository.DeactivateWithAuditCallCount);
    }

    [Fact]
    [Trait("UserStory", "US16")]
    public async Task DeactivateAsync_WhenDatabaseRejectsAdministrator_IsUnauthorized()
    {
        var repository = new FakeUserRepository
        {
            DeactivationStatus = AdminUserDeactivationStatus.AdministratorNotAuthorized
        };
        var service = CreateService(repository, userId: 1, isAdministrator: true);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.DeactivateAsync(42));
    }

    [Fact]
    [Trait("UserStory", "US16")]
    public async Task ReactivateAsync_AsAdministrator_PassesActorAndCorrelationId()
    {
        var repository = new FakeUserRepository();
        var service = CreateService(repository, userId: 1, isAdministrator: true);

        await service.ReactivateAsync(42);

        Assert.Equal(1, repository.ReactivateWithAuditCallCount);
        Assert.Equal(42, repository.LastReactivatedUserId);
        Assert.Equal(1, repository.LastReactivationAdministratorUserId);
        Assert.NotNull(repository.LastReactivationCorrelationId);
        Assert.NotEqual(Guid.Empty, repository.LastReactivationCorrelationId);
    }

    [Fact]
    [Trait("UserStory", "US16")]
    public async Task ReactivateAsync_WithInvalidId_IsRejectedBeforeRepository()
    {
        var repository = new FakeUserRepository();
        var service = CreateService(repository, userId: 1, isAdministrator: true);

        await Assert.ThrowsAsync<ValidationException>(() => service.ReactivateAsync(0));

        Assert.Equal(0, repository.ReactivateWithAuditCallCount);
    }

    [Theory]
    [InlineData(AdminUserActivationStatus.UserNotFound, "findes ikke")]
    [InlineData(AdminUserActivationStatus.AlreadyActive, "allerede aktiv")]
    [Trait("UserStory", "US16")]
    public async Task ReactivateAsync_WhenBusinessRuleRejects_ShowsSafeError(AdminUserActivationStatus status, string expectedMessage)
    {
        var repository = new FakeUserRepository
        {
            ActivationStatus = status
        };
        var service = CreateService(repository, userId: 1, isAdministrator: true);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReactivateAsync(42));

        Assert.Contains(expectedMessage, exception.Message);
        Assert.Equal(1, repository.ReactivateWithAuditCallCount);
    }

    [Fact]
    [Trait("UserStory", "US16")]
    public async Task ReactivateAsync_WhenDatabaseRejectsAdministrator_IsUnauthorized()
    {
        var repository = new FakeUserRepository
        {
            ActivationStatus = AdminUserActivationStatus.AdministratorNotAuthorized
        };
        var service = CreateService(repository, userId: 1, isAdministrator: true);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ReactivateAsync(42));
    }

    private static AdminUserService CreateService(FakeUserRepository repository, int? userId, bool isAdministrator)
    {
        return new AdminUserService(repository, new FakeCurrentUserService { UserId = userId, IsAdministrator = isAdministrator });
    }
}