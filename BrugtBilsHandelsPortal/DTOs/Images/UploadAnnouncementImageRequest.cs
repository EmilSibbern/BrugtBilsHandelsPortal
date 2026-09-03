namespace BrugtBilsHandelsPortal.DTOs.Images;

public class UploadAnnouncementImageRequest
{
    public int AnnouncementId { get; init; }

    public Stream Content { get; init; } = Stream.Null;

    public long Size { get; init; }

    public string ContentType { get; init; } = string.Empty;
}