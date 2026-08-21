namespace BrugtBilsHandelsPortal.DTOs.Users;

public class ProfileUpdateResult
{
    public bool Succeeded { get; set; }
    public string? ErrorMessage { get; set; }

    public static ProfileUpdateResult Successful()
    {
        return new ProfileUpdateResult
        {
            Succeeded = true
        };
    }

    public static ProfileUpdateResult Failed(string errorMessage)
    {
        return new ProfileUpdateResult
        {
            Succeeded = false,
            ErrorMessage = errorMessage
        };
    }
}