using System.Security.Claims;
using BrugtBilsHandelsPortal.Data.Repositories.Interfaces;
using BrugtBilsHandelsPortal.Models.Entities;
using Microsoft.AspNetCore.Components.Authorization;

namespace BrugtBilsHandelsPortal.Security;

/// <summary>
/// Læser den aktuelle identitet fra Blazor og kontrollerer,
/// at kontoen stadig er aktiv i databasen.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly AuthenticationStateProvider
        _authenticationStateProvider;

    private readonly IUserRepository
        _userRepository;

    public CurrentUserService(
        AuthenticationStateProvider authenticationStateProvider,
        IUserRepository userRepository)
    {
        _authenticationStateProvider =
            authenticationStateProvider;

        _userRepository =
            userRepository;
    }

    public async Task<int?> GetUserIdAsync()
    {
        var activeUser =
            await GetActiveUserAsync();

        return activeUser?.Id;
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var activeUser =
            await GetActiveUserAsync();

        return activeUser is not null;
    }

    public async Task<bool> IsInRoleAsync(
        string role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return false;
        }

        var activeUser =
            await GetActiveUserAsync();

        if (activeUser is null)
        {
            return false;
        }

        // Rollen sammenlignes med databasen i stedet for
        // kun at stole på cookie-claimet.
        return string.Equals(
            activeUser.UserGroupName,
            role,
            StringComparison.Ordinal);
    }

    /// <summary>
    /// Returnerer kun brugeren, hvis authentication-state
    /// indeholder et gyldigt ID, og kontoen stadig er aktiv.
    /// </summary>
    private async Task<User?>
        GetActiveUserAsync()
    {
        var authenticationState =
            await _authenticationStateProvider
                .GetAuthenticationStateAsync();

        var principal =
            authenticationState.User;

        if (principal.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var userIdValue =
            principal.FindFirst(
                    ClaimTypes.NameIdentifier)?
                .Value;

        if (!int.TryParse(
            userIdValue,
            out var userId))
        {
            return null;
        }

        // GetByIdAsync returnerer kun aktive brugere.
        return await _userRepository
            .GetByIdAsync(userId);
    }
}