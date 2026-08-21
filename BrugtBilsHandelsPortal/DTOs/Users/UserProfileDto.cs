namespace BrugtBilsHandelsPortal.DTOs.Users;

public class UserProfileDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string UserGroupName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}