namespace BrugtBilsHandelsPortal.DTOs.Users;

public sealed class AdminUserDto
{
	public int Id { get; init; }
	public string Username { get; init; } = string.Empty;
	public string Email { get; init; } = string.Empty;
	public string Role { get; init; } = string.Empty;
	public bool IsActive { get; init; }
	public DateTime CreatedAt { get; init; }
}
