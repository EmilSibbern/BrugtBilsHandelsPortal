namespace BrugtBilsHandelsPortal.DTOs.Authentication;

public class AuthenticationResult
{
    public bool Succeeded { get; set; }
    public string? ErrorMessage { get; set; }
    public int? UserId { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }

    public static AuthenticationResult Successful(int userId, string username, string email, string role)
    {
        return new AuthenticationResult
        {
            Succeeded = true,
            UserId = userId,
            Username = username,
            Email = email,
            Role = role
        };
    }

    public static AuthenticationResult Failed(string errorMessage)
    {
        return new AuthenticationResult
        {
            Succeeded = false,
            ErrorMessage = errorMessage
        };
    }
}