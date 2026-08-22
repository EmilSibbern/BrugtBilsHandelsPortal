using BrugtBilsHandelsPortal.DTOs.Announcements;
using BrugtBilsHandelsPortal.Models.Entities;

namespace BrugtBilsHandelsPortal.Services.Interfaces;

public interface IAnnouncementService
{
    Task<IReadOnlyList<CarBrand>> GetCarBrandsAsync();

    Task<CreateAnnouncementResult> CreateAsync(
        CreateAnnouncementDto request);
    
    Task<AnnouncementDetailsDto?> GetDetailsAsync(
        int announcementId);
    
    Task<AnnouncementSearchResult> SearchActiveAsync(
        AnnouncementSearchDto search);
}