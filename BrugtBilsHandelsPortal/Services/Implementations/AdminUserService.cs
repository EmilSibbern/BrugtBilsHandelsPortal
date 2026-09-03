using System.ComponentModel.DataAnnotations;
using BrugtBilsHandelsPortal.Data.Repositories.Interfaces;
using BrugtBilsHandelsPortal.DTOs.Users;
using BrugtBilsHandelsPortal.Security;
using BrugtBilsHandelsPortal.Services.Interfaces;

namespace BrugtBilsHandelsPortal.Services.Implementations;

/// <summary>
/// Håndhæver validering og server-side administratoradgang
/// til brugeradministration.
/// </summary>
public class AdminUserService : IAdminUserService
{
    private readonly IUserRepository _userRepository;

    private readonly ICurrentUserService _currentUserService;

    public AdminUserService(IUserRepository userRepository, ICurrentUserService currentUserService)
    {
        _userRepository = userRepository;
        _currentUserService = currentUserService;
    }

    public async Task<AdminUserSearchResult> SearchAsync(AdminUserSearchDto search)
    {
        ArgumentNullException.ThrowIfNull(search);

        // Kontrollér administratoradgangen før data hentes.
        await GetAdministratorUserIdAsync();

        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(search, new ValidationContext(search), validationResults, validateAllProperties: true);

        if (!isValid)
        {
            return AdminUserSearchResult.Failed(validationResults.First().ErrorMessage ?? "Søgekriterierne er ugyldige.");
        }

        // Normalisér værdierne, før repository-laget kaldes.
        search.Query = string.IsNullOrWhiteSpace(search.Query) ? null : search.Query.Trim();

        search.Role = string.IsNullOrWhiteSpace(search.Role) ? null : search.Role.Trim();

        var page = await _userRepository.SearchForAdministrationAsync(search);

        return AdminUserSearchResult.Successful(page);
    }

    public async Task DeactivateAsync(int targetUserId)
    {
        // Authorization foretages først, så anonyme eller
        // almindelige brugere ikke får oplysninger om,
        // hvorvidt et bestemt bruger-id eksisterer.
        var administratorUserId = await GetAdministratorUserIdAsync();

        if (targetUserId <= 0)
        {
            throw new ValidationException("Brugeren er ugyldig.");
        }

        // Ét correlation ID følger hele adminhandlingen
        // og gemmes sammen med den atomiske auditpost.
        var correlationId = Guid.NewGuid();

        var status = await _userRepository.DeactivateWithAuditAsync(targetUserId, administratorUserId, correlationId);

        switch (status)
        {
            case AdminUserDeactivationStatus.Succeeded:
                return;

            case AdminUserDeactivationStatus.UserNotFound:
                throw new InvalidOperationException("Brugeren findes ikke.");

            case AdminUserDeactivationStatus.AlreadyInactive:
                throw new InvalidOperationException("Brugeren er allerede deaktiveret.");

            case AdminUserDeactivationStatus.CannotDeactivateSelf:
                throw new InvalidOperationException("Du kan ikke deaktivere din egen konto.");

            case AdminUserDeactivationStatus.LastActiveAdministrator:
                throw new InvalidOperationException("Den sidste aktive administrator kan ikke deaktiveres.");

            case AdminUserDeactivationStatus.AdministratorNotAuthorized:
                throw new UnauthorizedAccessException("Du har ikke adgang til brugeradministration.");

            default:
                throw new InvalidOperationException("Brugeren kunne ikke deaktiveres.");
        }
    }

    public async Task ReactivateAsync(int targetUserId)
    {
        // Authorization udføres før øvrig validering,
        // så uvedkommende ikke får oplysninger om brugere.
        var administratorUserId = await GetAdministratorUserIdAsync();

        if (targetUserId <= 0)
        {
            throw new ValidationException("Brugeren er ugyldig.");
        }

        var correlationId = Guid.NewGuid();

        var status = await _userRepository.ReactivateWithAuditAsync(targetUserId, administratorUserId, correlationId);

        switch (status)
        {
            case AdminUserActivationStatus.Succeeded:
                return;

            case AdminUserActivationStatus.UserNotFound:
                throw new InvalidOperationException("Brugeren findes ikke.");

            case AdminUserActivationStatus.AlreadyActive:
                throw new InvalidOperationException("Brugeren er allerede aktiv.");

            case AdminUserActivationStatus.AdministratorNotAuthorized:
                throw new UnauthorizedAccessException("Du har ikke adgang til brugeradministration.");

            default:
                throw new InvalidOperationException("Brugeren kunne ikke genaktiveres.");
        }
    }

    /// <summary>
    /// Returnerer den aktuelle administrators bruger-id
    /// eller afviser handlingen.
    /// </summary>
    private async Task<int> GetAdministratorUserIdAsync()
    {
        var isAuthenticated = await _currentUserService.IsAuthenticatedAsync();

        if (!isAuthenticated)
        {
            throw new UnauthorizedAccessException("Du skal være logget ind.");
        }

        var userId = await _currentUserService.GetUserIdAsync();

        if (userId is null)
        {
            throw new UnauthorizedAccessException("Brugeridentiteten er ugyldig.");
        }

        var isAdministrator = await _currentUserService.IsInRoleAsync("Administrator");

        if (!isAdministrator)
        {
            throw new UnauthorizedAccessException("Du har ikke adgang til brugeradministration.");
        }

        return userId.Value;
    }
}