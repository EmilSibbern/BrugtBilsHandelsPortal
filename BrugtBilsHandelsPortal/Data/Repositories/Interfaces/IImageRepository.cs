using BrugtBilsHandelsPortal.DTOs.Images;
using BrugtBilsHandelsPortal.Models.Entities;

namespace BrugtBilsHandelsPortal.Data.Repositories.Interfaces;

/// <summary>Definerer ejerbegrænsede ændringer af billedmetadata.</summary>
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

    Task<string?> DeleteForOwnerAsync(
        int announcementId,
        int imageId,
        int userId);
}
