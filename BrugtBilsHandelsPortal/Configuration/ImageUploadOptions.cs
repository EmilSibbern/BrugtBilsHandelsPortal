namespace BrugtBilsHandelsPortal.Configuration;

public class ImageUploadOptions
{
    public const string SectionName = "ImageUpload";

    public long MaxFileSizeBytes { get; set; }
        = 5 * 1024 * 1024;

    public int MaxImagesPerAnnouncement { get; set; }
        = 10;

    public string UploadDirectory { get; set; }
        = "uploads";
}