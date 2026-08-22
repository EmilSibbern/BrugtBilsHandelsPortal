namespace BrugtBilsHandelsPortal.Models.Entities;

public class AnnouncementImage
{
    public int Id { get; set; }
    public int AnnouncementId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }
}