using BrugtBilsHandelsPortal.DTOs.Images;

namespace BrugtBilsHandelsPortal.Services.Interfaces;

public interface IImageService
{
    Task<ImageUploadResult> UploadAsync(
        UploadAnnouncementImageRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AnnouncementImageDto>>
        GetForActiveAnnouncementAsync(
            int announcementId);

    Task<bool> SetPrimaryAsync(
        int announcementId,
        int imageId);
}
