namespace BrugtBilsHandelsPortal.DTOs.Images;

public class AnnouncementImageDto
{
    public int Id { get; set; }
    public int AnnouncementId { get; set; }

    public string FileName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;

    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
}