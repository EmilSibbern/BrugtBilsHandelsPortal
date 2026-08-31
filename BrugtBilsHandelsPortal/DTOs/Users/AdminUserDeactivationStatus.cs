namespace BrugtBilsHandelsPortal.DTOs.Users;

/// <summary>
/// Mulige resultater af en atomisk administrativ
/// brugerdeaktivering.
/// </summary>
public enum AdminUserDeactivationStatus
{
    Succeeded,

    UserNotFound,

    AlreadyInactive,

    CannotDeactivateSelf,

    LastActiveAdministrator,

    AdministratorNotAuthorized
}
