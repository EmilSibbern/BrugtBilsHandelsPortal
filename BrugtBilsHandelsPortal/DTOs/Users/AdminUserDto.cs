namespace BrugtBilsHandelsPortal.DTOs.Users;

/// <summary>
/// Indeholder de kontooplysninger, som en administrator
/// har behov for ved brugeradministration.
/// </summary>
public class AdminUserDto
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}