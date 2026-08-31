namespace BrugtBilsHandelsPortal.Models.Entities;

/// <summary>
/// Registrerer en sikkerheds- eller administrationshandling
/// uden at gemme følsomt indhold.
/// </summary>
public class AuditLog
{
    public long Id { get; set; }

    public int? UserId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public string? EntityId { get; set; }

    public string? Details { get; set; }

    public Guid? CorrelationId { get; set; }

    public DateTime CreatedAt { get; set; }
}