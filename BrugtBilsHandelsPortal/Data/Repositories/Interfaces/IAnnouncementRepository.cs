using BrugtBilsHandelsPortal.DTOs.Announcements;
using BrugtBilsHandelsPortal.DTOs.Common;
using BrugtBilsHandelsPortal.Models.Entities;

namespace BrugtBilsHandelsPortal.Data.Repositories.Interfaces;

public interface IAnnouncementRepository
{
    Task<int> CreateAsync(
        Car car,
        Announcement announcement);
    
    Task<AnnouncementDetailsDto?> GetActiveByIdAsync(
        int announcementId);
    
    Task<PagedResult<AnnouncementListItemDto>>
        SearchActiveAsync(
            AnnouncementSearchDto search);
}