using BrugtBilsHandelsPortal.DTOs.Common;

namespace BrugtBilsHandelsPortal.DTOs.Announcements;

public record AdminAnnouncementSearchResult(bool Succeeded, PagedResult<AdminAnnouncementDto>? Page, string? ErrorMessage)
{
    public static AdminAnnouncementSearchResult Successful(PagedResult<AdminAnnouncementDto> page)
    {
        return new AdminAnnouncementSearchResult(true, page, null);
    }

    public static AdminAnnouncementSearchResult Failed(string errorMessage)
    {
        return new AdminAnnouncementSearchResult(false, null, errorMessage);
    }
}