using BrugtBilsHandelsPortal.Models.Entities;

namespace BrugtBilsHandelsPortal.DTOs.Images;

public enum ImageMetadataCreateStatus
{
    Success,
    AnnouncementNotFoundOrNotOwned,
    ImageLimitReached
}

public record ImageMetadataCreateResult(
    ImageMetadataCreateStatus Status,
    AnnouncementImage? Image)
{
    public static ImageMetadataCreateResult Successful(
        AnnouncementImage image)
    {
        return new ImageMetadataCreateResult(
            ImageMetadataCreateStatus.Success,
            image);
    }

    public static ImageMetadataCreateResult Failed(
        ImageMetadataCreateStatus status)
    {
        return new ImageMetadataCreateResult(
            status,
            null);
    }
}