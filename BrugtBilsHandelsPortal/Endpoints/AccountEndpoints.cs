using System.Globalization;
using System.Security.Claims;
using BrugtBilsHandelsPortal.DTOs.Authentication;
using BrugtBilsHandelsPortal.Services.Interfaces;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace BrugtBilsHandelsPortal.Endpoints;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
            "/account/login",
            LoginAsync);
        endpoints.MapPost(
                "/account/logout",
                LogoutAsync)
            .RequireAuthorization();
        return endpoints;
    }

    private static async Task<IResult> LoginAsync(
        HttpContext httpContext,
        IAuthService authService,
        IAntiforgery antiforgery)
    {
        await antiforgery.ValidateRequestAsync(
            httpContext);

        var form = await httpContext.Request
            .ReadFormAsync(
                httpContext.RequestAborted);

        var request = new LoginRequest
        {
            EmailOrUsername =
                form["EmailOrUsername"].ToString(),

            Password =
                form["Password"].ToString(),

            RememberMe =
                string.Equals(
                    form["RememberMe"].ToString(),
                    "true",
                    StringComparison.OrdinalIgnoreCase)
        };

        var result =
            await authService.LoginAsync(request);

        if (!result.Succeeded ||
            result.UserId is null ||
            string.IsNullOrWhiteSpace(result.Username) ||
            string.IsNullOrWhiteSpace(result.Email) ||
            string.IsNullOrWhiteSpace(result.Role))
        {
            return Results.LocalRedirect(
                "/Login?loginFailed=true");
        }

        var claims = new List<Claim>
        {
            new(
                ClaimTypes.NameIdentifier,
                result.UserId.Value.ToString(
                    CultureInfo.InvariantCulture)),

            new(
                ClaimTypes.Name,
                result.Username),

            new(
                ClaimTypes.Email,
                result.Email),

            new(
                ClaimTypes.Role,
                result.Role)
        };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults
                .AuthenticationScheme);

        var principal =
            new ClaimsPrincipal(identity);

        var properties =
            new AuthenticationProperties
            {
                IsPersistent = request.RememberMe,
                AllowRefresh = true,

                ExpiresUtc = request.RememberMe
                    ? DateTimeOffset.UtcNow.AddDays(14)
                    : null
            };

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults
                .AuthenticationScheme,
            principal,
            properties);

        var returnUrl = GetLocalReturnUrl(
            form["ReturnUrl"].ToString());

        return Results.LocalRedirect(returnUrl);
    }

    private static string GetLocalReturnUrl(
        string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return "/";
        }

        if (!returnUrl.StartsWith(
                "/",
                StringComparison.Ordinal) ||
            returnUrl.StartsWith(
                "//",
                StringComparison.Ordinal) ||
            returnUrl.StartsWith(
                "/\\",
                StringComparison.Ordinal))
        {
            return "/";
        }

        return returnUrl;
    }
    
    private static async Task<IResult> LogoutAsync(
        HttpContext httpContext,
        IAntiforgery antiforgery)
    {
        await antiforgery.ValidateRequestAsync(
            httpContext);

        await httpContext.SignOutAsync(
            CookieAuthenticationDefaults
                .AuthenticationScheme);

        return Results.LocalRedirect("/");
    }
}