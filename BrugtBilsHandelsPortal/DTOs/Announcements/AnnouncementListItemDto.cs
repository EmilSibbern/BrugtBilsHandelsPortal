namespace BrugtBilsHandelsPortal.DTOs.Announcements;

public class AnnouncementListItemDto
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;
    public int Price { get; set; }
    public DateTime CreatedAt { get; set; }

    public string BrandName { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;

    public int ProductionYear { get; set; }
    public int Mileage { get; set; }

    public bool IsActive { get; set; }

    public string? PrimaryImageFileName { get; set; }
}