namespace BrugtBilsHandelsPortal.Models.Entities;

public class Announcement
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int CarId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public int Price { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public bool IsActive { get; set; }
}
