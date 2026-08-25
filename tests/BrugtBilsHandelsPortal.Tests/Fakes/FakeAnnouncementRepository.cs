using BrugtBilsHandelsPortal.Data.Repositories.Interfaces;
using BrugtBilsHandelsPortal.DTOs.Announcements;
using BrugtBilsHandelsPortal.DTOs.Common;
using BrugtBilsHandelsPortal.Models.Entities;

namespace BrugtBilsHandelsPortal.Tests.Fakes;

public class FakeAnnouncementRepository : IAnnouncementRepository
{
    public int AnnouncementIdToReturn { get; set; } = 1;

    public int CreateCallCount { get; private set; }
    public int GetByIdCallCount { get; private set; }
    public int GetActiveByIdCallCount { get; private set; }
    public int SearchActiveCallCount { get; private set; }
    public int? LastRequestedAnnouncementId { get; private set; }
    public AnnouncementSearchDto? LastSearch { get; private set; }

    public Car? CreatedCar { get; private set; }
    public Announcement? CreatedAnnouncement { get; private set; }

    public Dictionary<int, AnnouncementDetailsDto> Details { get; } = [];
    public List<AnnouncementListItemDto> ActiveAnnouncements { get; } = [];

    public Task<int> CreateAsync(
        Car car,
        Announcement announcement)
    {
        CreateCallCount++;
        CreatedCar = car;
        CreatedAnnouncement = announcement;

        return Task.FromResult(AnnouncementIdToReturn);
    }

    public Task<AnnouncementDetailsDto?> GetActiveByIdAsync(
        int announcementId)
    {
        GetActiveByIdCallCount++;
        LastRequestedAnnouncementId = announcementId;

        Details.TryGetValue(
            announcementId,
            out var announcement);

        return Task.FromResult(
            announcement?.IsActive == true
                ? announcement
                : null);
    }

    public Task<AnnouncementDetailsDto?> GetByIdAsync(
        int announcementId)
    {
        GetByIdCallCount++;
        LastRequestedAnnouncementId = announcementId;

        Details.TryGetValue(
            announcementId,
            out var announcement);

        return Task.FromResult(announcement);
    }

    public Task<PagedResult<AnnouncementListItemDto>>
        SearchActiveAsync(
            AnnouncementSearchDto search)
    {
        SearchActiveCallCount++;
        LastSearch = search;

        return Task.FromResult(
            new PagedResult<AnnouncementListItemDto>
            {
                Items = ActiveAnnouncements,
                Page = search.Page,
                PageSize = search.PageSize,
                TotalItems = ActiveAnnouncements.Count
            });
    }
}
