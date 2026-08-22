using BrugtBilsHandelsPortal.DTOs.Images;
using BrugtBilsHandelsPortal.Models.Entities;

namespace BrugtBilsHandelsPortal.Data.Repositories.Interfaces;

public interface IImageRepository
{
    Task<ImageMetadataCreateResult> AddForOwnerAsync(
        int announcementId,
        int userId,
        string fileName,
        int maxImages);

    Task<IReadOnlyList<AnnouncementImage>>
        GetForActiveAnnouncementAsync(
            int announcementId);

    Task<bool> SetPrimaryForOwnerAsync(
        int announcementId,
        int imageId,
        int userId);
}
