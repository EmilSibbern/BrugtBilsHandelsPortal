using BrugtBilsHandelsPortal.DTOs.Common;

namespace BrugtBilsHandelsPortal.DTOs.Users;

public record AdminUserSearchResult(bool Succeeded, PagedResult<AdminUserDto>? Page, string? ErrorMessage)
{
    public static AdminUserSearchResult Successful(PagedResult<AdminUserDto> page)
    {
        return new AdminUserSearchResult(true, page, null);
    }

    public static AdminUserSearchResult Failed(string errorMessage)
    {
        return new AdminUserSearchResult(false, null, errorMessage);
    }
}