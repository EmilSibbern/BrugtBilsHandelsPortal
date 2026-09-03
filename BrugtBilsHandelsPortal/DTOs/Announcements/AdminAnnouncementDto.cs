namespace BrugtBilsHandelsPortal.DTOs.Announcements;

/// <summary>
/// De annonceoplysninger en administrator skal bruge
/// til oversigt og moderation.
/// </summary>
public class AdminAnnouncementDto
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public int Price { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int OwnerUserId { get; set; }

    public string OwnerUsername { get; set; } = string.Empty;

    public string BrandName { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public int ProductionYear { get; set; }

    public string? PrimaryImageFileName { get; set; }

    public int ImageCount { get; set; }
}