namespace BrugtBilsHandelsPortal.Models.Entities;

public class Car
{
    public int CarId { get; set; }
    public int BrandId { get; set; }

    public string Model { get; set; } = string.Empty;

    public short ProductionYear { get; set; }
    public int Mileage { get; set; }

    public string? Color { get; set; }
}