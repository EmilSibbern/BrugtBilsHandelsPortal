using System.Reflection;
using BrugtBilsHandelsPortal.Components.Pages.Admin;
using BrugtBilsHandelsPortal.DTOs.Common;
using BrugtBilsHandelsPortal.DTOs.Users;
using BrugtBilsHandelsPortal.Security;
using BrugtBilsHandelsPortal.Services.Interfaces;
using BrugtBilsHandelsPortal.Tests.Fakes;
using Bunit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BrugtBilsHandelsPortal.Tests.Components;

public class ManageUsersTests
{
    [Fact]
    [Trait("UserStory", "US16")]
    public void Component_RequiresAdministratorRole()
    {
        var attribute = Assert.Single(
            typeof(ManageUsers)
                .GetCustomAttributes<AuthorizeAttribute>());

        Assert.Equal("Administrator", attribute.Roles);
    }

    [Fact]
    [Trait("UserStory", "US16")]
    public void Administrator_SeesNonSensitiveUserList()
    {
        using var context = CreateContext(
            CreateCurrentAdministrator(),
            CreateOrdinaryUser());

        var cut = RenderPage(context);

        cut.WaitForAssertion(() =>
        {
            var table = cut.Find("[data-testid='admin-users-table']");
            Assert.Contains("administrator", table.TextContent);
            Assert.Contains("anna", table.TextContent);
            Assert.Contains("anna@example.dk", table.TextContent);
            Assert.DoesNotContain("PasswordHash", cut.Markup);
        });
    }

    [Fact]
    [Trait("UserStory", "US16")]
    public void EmptySearch_ShowsEmptyState()
    {
        using var context = CreateContext();

        var cut = RenderPage(context);

        cut.WaitForAssertion(() =>
            Assert.NotNull(
                cut.Find("[data-testid='admin-users-empty']")));
    }

    [Fact]
    [Trait("UserStory", "US16")]
    public void SearchForm_SendsQueryStatusAndRoleToService()
    {
        using var context = CreateContext(
            CreateOrdinaryUser());
        var service = GetService(context);
        var cut = RenderPage(context);

        cut.Find("#user-search").Change("anna");
        cut.Find("#status-filter").Change("true");
        cut.Find("#role-filter").Change("User");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(2, service.SearchCallCount);
            Assert.NotNull(service.LastSearch);
            Assert.Equal("anna", service.LastSearch.Query);
            Assert.True(service.LastSearch.IsActive);
            Assert.Equal("User", service.LastSearch.Role);
            Assert.Equal(1, service.LastSearch.Page);
        });
    }

    [Fact]
    [Trait("UserStory", "US16")]
    public void CurrentAdministrator_CannotOpenSelfDeactivation()
    {
        using var context = CreateContext(
            CreateCurrentAdministrator(),
            CreateOrdinaryUser());

        var cut = RenderPage(context);

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Dig", cut.Markup);
            Assert.Single(
                cut.FindAll("[data-testid='open-deactivate-user']"));
            Assert.Contains("Kan ikke deaktiveres", cut.Markup);
        });
    }

    [Fact]
    [Trait("UserStory", "US16")]
    public void DeactivateAction_ShowsAndCancelsConfirmation()
    {
        using var context = CreateContext(
            CreateOrdinaryUser());
        var cut = RenderPage(context);

        cut.WaitForElement(
                "[data-testid='open-deactivate-user']")
            .Click();

        Assert.Contains(
            "anna",
            cut.Find("[data-testid='deactivate-user-confirmation']")
                .TextContent);

        cut.Find("[data-testid='cancel-deactivate-user']").Click();

        Assert.Empty(
            cut.FindAll("[data-testid='deactivate-user-confirmation']"));
    }

    [Fact]
    [Trait("UserStory", "US16")]
    public void ConfirmDeactivate_UpdatesListAndShowsSuccess()
    {
        using var context = CreateContext(
            CreateOrdinaryUser());
        var service = GetService(context);
        var cut = RenderPage(context);

        cut.WaitForElement(
                "[data-testid='open-deactivate-user']")
            .Click();
        cut.Find("[data-testid='confirm-deactivate-user']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(1, service.DeactivateCallCount);
            Assert.Equal(42, service.LastDeactivatedUserId);
            Assert.Contains(
                "anna er deaktiveret",
                cut.Find("[data-testid='admin-user-success']")
                    .TextContent);
            Assert.Contains("Inaktiv", cut.Markup);
            Assert.Empty(
                cut.FindAll("[data-testid='open-deactivate-user']"));
        });
    }

    [Fact]
    [Trait("UserStory", "US16")]
    public void ConfirmDeactivate_WhenServiceFails_ShowsSafeError()
    {
        using var context = CreateContext(
            CreateOrdinaryUser());
        var service = GetService(context);
        service.DeactivateException = new InvalidOperationException(
            "Den sidste aktive administrator kan ikke deaktiveres.");
        var cut = RenderPage(context);

        cut.WaitForElement(
                "[data-testid='open-deactivate-user']")
            .Click();
        cut.Find("[data-testid='confirm-deactivate-user']").Click();

        cut.WaitForAssertion(() =>
        {
            var error = cut.Find("[data-testid='admin-user-error']");
            Assert.Contains(
                "sidste aktive administrator",
                error.TextContent);
            Assert.NotNull(
                cut.Find("[data-testid='open-deactivate-user']"));
        });
    }

    [Fact]
    [Trait("UserStory", "US16")]
    public void InactiveUser_SeesReactivateActionInsteadOfDeactivateAction()
    {
        using var context = CreateContext(
            CreateInactiveUser());
        var cut = RenderPage(context);

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(
                cut.Find("[data-testid='open-reactivate-user']"));
            Assert.Empty(
                cut.FindAll("[data-testid='open-deactivate-user']"));
        });
    }

    [Fact]
    [Trait("UserStory", "US16")]
    public void ReactivateAction_ShowsAndCancelsConfirmation()
    {
        using var context = CreateContext(
            CreateInactiveUser());
        var cut = RenderPage(context);

        cut.WaitForElement(
                "[data-testid='open-reactivate-user']")
            .Click();

        Assert.Contains(
            "anna",
            cut.Find("[data-testid='reactivate-user-confirmation']")
                .TextContent);

        cut.Find("[data-testid='cancel-reactivate-user']").Click();

        Assert.Empty(
            cut.FindAll("[data-testid='reactivate-user-confirmation']"));
    }

    [Fact]
    [Trait("UserStory", "US16")]
    public void ConfirmReactivate_UpdatesListAndShowsSuccess()
    {
        using var context = CreateContext(
            CreateInactiveUser());
        var service = GetService(context);
        var cut = RenderPage(context);

        cut.WaitForElement(
                "[data-testid='open-reactivate-user']")
            .Click();
        cut.Find("[data-testid='confirm-reactivate-user']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(1, service.ReactivateCallCount);
            Assert.Equal(42, service.LastReactivatedUserId);
            Assert.Contains(
                "anna er genaktiveret",
                cut.Find("[data-testid='admin-user-success']")
                    .TextContent);
            Assert.Contains("Aktiv", cut.Markup);
            Assert.NotNull(
                cut.Find("[data-testid='open-deactivate-user']"));
            Assert.Empty(
                cut.FindAll("[data-testid='open-reactivate-user']"));
        });
    }

    [Fact]
    [Trait("UserStory", "US16")]
    public void ConfirmReactivate_WhenServiceFails_ShowsSafeError()
    {
        using var context = CreateContext(
            CreateInactiveUser());
        var service = GetService(context);
        service.ReactivateException = new InvalidOperationException(
            "Brugeren er allerede aktiv.");
        var cut = RenderPage(context);

        cut.WaitForElement(
                "[data-testid='open-reactivate-user']")
            .Click();
        cut.Find("[data-testid='confirm-reactivate-user']").Click();

        cut.WaitForAssertion(() =>
        {
            var error = cut.Find("[data-testid='admin-user-error']");
            Assert.Contains("allerede aktiv", error.TextContent);
            Assert.NotNull(
                cut.Find("[data-testid='open-reactivate-user']"));
        });
    }

    private static BunitContext CreateContext(
        params AdminUserDto[] users)
    {
        var context = new BunitContext();
        var authorization = context.AddAuthorization();
        authorization.SetAuthorized("administrator");
        authorization.SetRoles("Administrator");

        var service = new StubAdminUserService();
        service.Users.AddRange(users);

        context.Services.AddSingleton<IAdminUserService>(service);
        context.Services.AddSingleton<ICurrentUserService>(
            new FakeCurrentUserService
            {
                UserId = 1,
                IsAdministrator = true
            });

        return context;
    }

    private static StubAdminUserService GetService(
        BunitContext context)
    {
        return Assert.IsType<StubAdminUserService>(
            context.Services.GetRequiredService<IAdminUserService>());
    }

    private static IRenderedComponent<ManageUsers> RenderPage(
        BunitContext context)
    {
        return context.Render<ManageUsers>();
    }

    private static AdminUserDto CreateCurrentAdministrator()
    {
        return new AdminUserDto
        {
            Id = 1,
            Username = "administrator",
            Email = "admin@example.dk",
            Role = "Administrator",
            IsActive = true,
            CreatedAt = new DateTime(2026, 8, 1)
        };
    }

    private static AdminUserDto CreateOrdinaryUser()
    {
        return new AdminUserDto
        {
            Id = 42,
            Username = "anna",
            Email = "anna@example.dk",
            Role = "User",
            IsActive = true,
            CreatedAt = new DateTime(2026, 8, 20)
        };
    }

    private static AdminUserDto CreateInactiveUser()
    {
        var user = CreateOrdinaryUser();
        user.IsActive = false;

        return user;
    }

    private sealed class StubAdminUserService : IAdminUserService
    {
        public List<AdminUserDto> Users { get; } = [];
        public int SearchCallCount { get; private set; }
        public int DeactivateCallCount { get; private set; }
        public int ReactivateCallCount { get; private set; }
        public int? LastDeactivatedUserId { get; private set; }
        public int? LastReactivatedUserId { get; private set; }
        public AdminUserSearchDto? LastSearch { get; private set; }
        public Exception? SearchException { get; set; }
        public Exception? DeactivateException { get; set; }
        public Exception? ReactivateException { get; set; }

        public Task<AdminUserSearchResult> SearchAsync(
            AdminUserSearchDto search)
        {
            SearchCallCount++;
            LastSearch = new AdminUserSearchDto
            {
                Query = search.Query,
                IsActive = search.IsActive,
                Role = search.Role,
                Page = search.Page,
                PageSize = search.PageSize
            };

            if (SearchException is not null)
            {
                return Task.FromException<AdminUserSearchResult>(
                    SearchException);
            }

            IReadOnlyList<AdminUserDto> items = Users.ToArray();

            return Task.FromResult(
                AdminUserSearchResult.Successful(
                    new PagedResult<AdminUserDto>
                    {
                        Items = items,
                        Page = search.Page,
                        PageSize = search.PageSize,
                        TotalItems = items.Count
                    }));
        }

        public Task DeactivateAsync(int targetUserId)
        {
            DeactivateCallCount++;
            LastDeactivatedUserId = targetUserId;

            if (DeactivateException is not null)
            {
                return Task.FromException(DeactivateException);
            }

            var user = Users.FirstOrDefault(
                candidate => candidate.Id == targetUserId);

            if (user is not null)
            {
                user.IsActive = false;
            }

            return Task.CompletedTask;
        }

        public Task ReactivateAsync(int targetUserId)
        {
            ReactivateCallCount++;
            LastReactivatedUserId = targetUserId;

            if (ReactivateException is not null)
            {
                return Task.FromException(ReactivateException);
            }

            var user = Users.FirstOrDefault(
                candidate => candidate.Id == targetUserId);

            if (user is not null)
            {
                user.IsActive = true;
            }

            return Task.CompletedTask;
        }
    }
}
