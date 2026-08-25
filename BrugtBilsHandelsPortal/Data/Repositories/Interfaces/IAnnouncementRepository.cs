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

    /// <summary>Henter kun annoncen, når den og sælgeren er aktive.</summary>
    Task<AnnouncementDetailsDto?> GetActiveByIdAsync(
        int announcementId);
    
    /// <summary>Søger og sideinddeler aktive annoncer.</summary>
    Task<PagedResult<AnnouncementListItemDto>>
        SearchActiveAsync(
            AnnouncementSearchDto search);

    /// <summary>Henter en annonce uanset aktiv-status til et efterfølgende autorisationstjek.</summary>
    Task<AnnouncementDetailsDto?> GetByIdAsync(
        int announcementId);
}
