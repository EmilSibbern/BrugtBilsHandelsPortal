namespace BrugtBilsHandelsPortal.DTOs.Images;

public record ImageUploadResult(bool Succeeded, AnnouncementImageDto? Image, string? ErrorMessage)
{
    public static ImageUploadResult Successful(AnnouncementImageDto image)
    {
        return new ImageUploadResult(true, image, null);
    }

    public static ImageUploadResult Failed(string errorMessage)
    {
        return new ImageUploadResult(false, null, errorMessage);
    }
}