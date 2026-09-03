namespace BrugtBilsHandelsPortal.Security;

/// <summary>
/// Giver service-laget adgang til den aktuelle Blazor-brugers identitet uden en direkte UI-afhængighed.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>Returnerer bruger-id'et fra claims eller <see langword="null"/> for en anonym bruger.</summary>
    Task<int?> GetUserIdAsync();
    /// <summary>Angiver, om den aktuelle identitet er godkendt.</summary>
    Task<bool> IsAuthenticatedAsync();
    /// <summary>Kontrollerer et rolle-claim for den aktuelle identitet.</summary>
    Task<bool> IsInRoleAsync(string role);
}