using BrugtBilsHandelsPortal.DTOs.Announcements;
using BrugtBilsHandelsPortal.DTOs.Common;
using BrugtBilsHandelsPortal.Models.Entities;

namespace BrugtBilsHandelsPortal.Data.Repositories.Interfaces;

/// <summary>Definerer persistensoperationer for annoncer og deres tilknyttede biler.</summary>
public interface IAnnouncementRepository
{
    /// <summary>Opretter bil og annonce som én atomisk operation.</summary>
    Task<int> CreateAsync(Car car, Announcement announcement);

    /// <summary>
    /// Opdaterer bil og annonce atomisk, hvis brugeren har adgang,
    /// og annoncen ikke er ændret siden formularen blev indlæst.
    /// </summary>
    Task<bool> UpdateAsync(Car car, Announcement announcement, int actorUserId, bool isAdministrator, DateTime? expectedUpdatedAt);

    /// <summary>Henter alle annoncer for den angivne bruger.</summary>
    Task<IReadOnlyList<AnnouncementListItemDto>> GetByUserIdAsync(int userId);

    /// <summary>
    /// Aktiverer en inaktiv annonce, hvis den tilhører den angivne bruger.
    /// </summary>
    Task<bool> ActivateForOwnerAsync(int announcementId, int ownerUserId);

    /// <summary>
    /// Deaktiverer en aktiv annonce, hvis den tilhører den angivne bruger.
    /// Returnerer false, hvis annoncen ikke findes, allerede er inaktiv
    /// eller tilhører en anden bruger.
    /// </summary>
    Task<bool> DeactivateForOwnerAsync(int announcementId, int ownerUserId);

    /// <summary>Henter kun annoncen, når den og sælgeren er aktive.</summary>
    Task<AnnouncementDetailsDto?> GetActiveByIdAsync(int announcementId);

    /// <summary>Søger og sideinddeler aktive annoncer.</summary>
    Task<PagedResult<AnnouncementListItemDto>> SearchActiveAsync(AnnouncementSearchDto search);

    /// <summary>Henter en annonce uanset aktiv-status til et efterfølgende autorisationstjek.</summary>
    Task<AnnouncementDetailsDto?> GetByIdAsync(int announcementId);

    /// <summary>
    /// Søger blandt alle annoncer til administratorens
    /// moderationsoversigt.
    /// </summary>
    Task<PagedResult<AdminAnnouncementDto>> SearchForAdministrationAsync(AdminAnnouncementSearchDto search);

    /// <summary>
    /// Opdaterer en annonce som administrator og opretter
    /// en auditlog i samme databasetransaktion.
    /// </summary>
    Task<bool> UpdateForAdministrationWithAuditAsync(Car car, Announcement announcement, int administratorUserId, Guid correlationId, DateTime? expectedUpdatedAt);

    /// <summary>
    /// Deaktiverer en annonce som administrator og opretter
    /// en auditlog i samme databasetransaktion.
    /// </summary>
    Task<bool> DeactivateForAdministrationWithAuditAsync(int announcementId, int administratorUserId, Guid correlationId);

    /// <summary>
    /// Henter en annonce uanset annoncens og ejerens status.
    /// Må kun kaldes efter administratorautorisation.
    /// </summary>
    Task<AnnouncementDetailsDto?> GetByIdForAdministrationAsync(int announcementId);

    /// <summary>
    /// Genaktiverer en annonce som administrator og opretter
    /// en auditlog i samme databasetransaktion.
    /// </summary>
    Task<bool> ReactivateForAdministrationWithAuditAsync(int announcementId, int administratorUserId, Guid correlationId);
}
