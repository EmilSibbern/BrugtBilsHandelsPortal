namespace BrugtBilsHandelsPortal.DTOs.Announcements;

public record CreateAnnouncementResult(bool Succeeded, int? AnnouncementId, string? ErrorMessage)
{
    public static CreateAnnouncementResult Successful(int announcementId)
    {
        return new CreateAnnouncementResult(true, announcementId, null);
    }

    public static CreateAnnouncementResult Failed(string errorMessage)
    {
        return new CreateAnnouncementResult(false, null, errorMessage);
    }
}