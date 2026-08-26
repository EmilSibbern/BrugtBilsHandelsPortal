using BrugtBilsHandelsPortal.DTOs.Announcements;
using BrugtBilsHandelsPortal.DTOs.Common;
using BrugtBilsHandelsPortal.Models.Entities;

namespace BrugtBilsHandelsPortal.Data.Repositories.Interfaces;

/// <summary>Definerer persistensoperationer for annoncer og deres tilknyttede biler.</summary>
public interface IAnnouncementRepository
{
    /// <summary>Opretter bil og annonce som én atomisk operation.</summary>
    Task<int> CreateAsync(
        Car car,
        Announcement announcement);

    /// <summary>
    /// Opdaterer bil og annonce atomisk, hvis brugeren har adgang,
    /// og annoncen ikke er ændret siden formularen blev indlæst.
    /// </summary>
    Task<bool> UpdateAsync(
        Car car,
        Announcement announcement,
        int actorUserId,
        bool isAdministrator,
        DateTime? expectedUpdatedAt);

    /// <summary>
    /// Deaktiverer en aktiv annonce, hvis den tilhører den angivne bruger.
    /// Returnerer false, hvis annoncen ikke findes, allerede er inaktiv
    /// eller tilhører en anden bruger.
    /// </summary>
    Task<bool> DeactivateForOwnerAsync(
        int announcementId,
        int ownerUserId);

    /// <summary>Henter kun annoncen, når den og sælgeren er aktive.</summary>
    Task<AnnouncementDetailsDto?> GetActiveByIdAsync(
        int announcementId);

    /// <summary>Søger og sideinddeler aktive annoncer.</summary>
    Task<PagedResult<AnnouncementListItemDto>> SearchActiveAsync(
        AnnouncementSearchDto search);

    /// <summary>Henter en annonce uanset aktiv-status til et efterfølgende autorisationstjek.</summary>
    Task<AnnouncementDetailsDto?> GetByIdAsync(
        int announcementId);
}