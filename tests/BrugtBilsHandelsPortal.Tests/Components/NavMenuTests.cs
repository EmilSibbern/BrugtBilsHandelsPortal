using BrugtBilsHandelsPortal.Components.Layout;
using Bunit;
using Bunit.TestDoubles;
using Xunit;

namespace BrugtBilsHandelsPortal.Tests.Components;

public class NavMenuTests
{
    [Fact]
    public void Guest_CanSeePublicNavigationButNotProtectedActions()
    {
        using var context = new BunitContext();
        context.AddAuthorization();

        var cut = context.Render<NavMenu>();

        Assert.NotNull(cut.Find("a[href='']"));
        Assert.Contains("Log ind", cut.Markup);
        Assert.Contains("Opret bruger", cut.Markup);
        Assert.DoesNotContain("Administration", cut.Markup);
        Assert.DoesNotContain("Opret announce", cut.Markup);
    }

    [Fact]
    public void User_DoesNotSeeAdministratorNavigation()
    {
        using var context = new BunitContext();
        var authorization = context.AddAuthorization();
        authorization.SetAuthorized("bruger");
        authorization.SetRoles("User");

        var cut = context.Render<NavMenu>();

        Assert.DoesNotContain("Administration", cut.Markup);
        Assert.NotNull(cut.Find("a[href='messages']"));
    }

    [Fact]
    public void Administrator_SeesAdministratorNavigation()
    {
        using var context = new BunitContext();
        var authorization = context.AddAuthorization();
        authorization.SetAuthorized("admin");
        authorization.SetRoles("Administrator");

        var cut = context.Render<NavMenu>();

        var adminLink = cut.Find("a[href='admin']");
        Assert.Equal("Administration", adminLink.TextContent.Trim());
    }
}