namespace BrugtBilsHandelsPortal.DTOs.Users;

/// <summary>
/// Mulige resultater af en atomisk administrativ
/// genaktivering af en brugerkonto.
/// </summary>
public enum AdminUserActivationStatus
{
    Succeeded,

    UserNotFound,

    AlreadyActive,

    AdministratorNotAuthorized
}