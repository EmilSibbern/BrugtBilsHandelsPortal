using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace BrugtBilsHandelsPortal.Security;

public class CurrentUserService : ICurrentUserService
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    public CurrentUserService(
        AuthenticationStateProvider authenticationStateProvider)
    {
        _authenticationStateProvider = authenticationStateProvider;
    }

    public async Task<int?> GetUserIdAsync()
    {
        var authenticationState =
            await _authenticationStateProvider.GetAuthenticationStateAsync();

        var userIdValue = authenticationState.User
            .FindFirst(ClaimTypes.NameIdentifier)?
            .Value;

        return int.TryParse(userIdValue, out var userId)
            ? userId
            : null;
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var authenticationState =
            await _authenticationStateProvider.GetAuthenticationStateAsync();

        return authenticationState.User.Identity?.IsAuthenticated == true;
    }
}