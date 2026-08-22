namespace BrugtBilsHandelsPortal.DTOs.Announcements;

public class AnnouncementDetailsDto
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public int Price { get; set; }
    public DateTime CreatedAt { get; set; }

    public int SellerUserId { get; set; }
    public string SellerUsername { get; set; } = string.Empty;

    public int CarId { get; set; }
    public int BrandId { get; set; }

    public string BrandName { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;

    public int ProductionYear { get; set; }
    public int Mileage { get; set; }

    public string? Color { get; set; }
}