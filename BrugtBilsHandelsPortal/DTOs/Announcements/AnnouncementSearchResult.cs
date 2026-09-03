using BrugtBilsHandelsPortal.DTOs.Common;

namespace BrugtBilsHandelsPortal.DTOs.Announcements;

public record AnnouncementSearchResult(bool Succeeded, PagedResult<AnnouncementListItemDto>? Page, string? ErrorMessage)
{
    public static AnnouncementSearchResult Successful(PagedResult<AnnouncementListItemDto> page)
    {
        return new AnnouncementSearchResult(true, page, null);
    }

    public static AnnouncementSearchResult Failed(string errorMessage)
    {
        return new AnnouncementSearchResult(false, null, errorMessage);
    }
}