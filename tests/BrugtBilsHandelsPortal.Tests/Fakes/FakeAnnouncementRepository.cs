using BrugtBilsHandelsPortal.Data.Repositories.Interfaces;
using BrugtBilsHandelsPortal.DTOs.Announcements;
using BrugtBilsHandelsPortal.DTOs.Common;
using BrugtBilsHandelsPortal.Models.Entities;

namespace BrugtBilsHandelsPortal.Tests.Fakes;

public class FakeAnnouncementRepository : IAnnouncementRepository
{
    public int AdminReactivateCallCount { get; private set; }
    public int? LastAdminReactivatedAnnouncementId { get; private set; }
    public bool AdminReactivateSucceeds { get; set; } = true;

    public Task<bool> ReactivateForAdministrationWithAuditAsync(int announcementId, int administratorUserId, Guid correlationId)
    {
        AdminReactivateCallCount++;
        LastAdminReactivatedAnnouncementId = announcementId;
        LastAdministratorUserId = administratorUserId;
        LastAdminCorrelationId = correlationId;
        return Task.FromResult(AdminReactivateSucceeds);
    }

    public int AnnouncementIdToReturn { get; set; } = 1;

    public int CreateCallCount { get; private set; }
    public int GetByIdCallCount { get; private set; }
    public int GetByIdForAdministrationCallCount { get; private set; }
    public HashSet<int> InactiveSellerUserIds { get; } = [];
    public int GetActiveByIdCallCount { get; private set; }
    public int SearchActiveCallCount { get; private set; }
    public int UpdateCallCount { get; private set; }
    public int GetByUserIdCallCount { get; private set; }
    public int ActivateCallCount { get; private set; }
    public int DeactivateCallCount { get; private set; }
    public int AdminSearchCallCount { get; private set; }
    public int AdminUpdateCallCount { get; private set; }
    public int AdminDeactivateCallCount { get; private set; }
    public int? LastRequestedAnnouncementId { get; private set; }
    public int? LastRequestedUserId { get; private set; }
    public AnnouncementSearchDto? LastSearch { get; private set; }
    public AdminAnnouncementSearchDto? LastAdminSearch { get; private set; }

    public Car? CreatedCar { get; private set; }
    public Announcement? CreatedAnnouncement { get; private set; }
    public Car? UpdatedCar { get; private set; }
    public Announcement? UpdatedAnnouncement { get; private set; }
    public int? LastUpdateActorUserId { get; private set; }
    public bool LastUpdateWasAdministrator { get; private set; }
    public DateTime? LastExpectedUpdatedAt { get; private set; }
    public bool UpdateSucceeds { get; set; } = true;
    public bool ActivateSucceeds { get; set; } = true;
    public bool DeactivateSucceeds { get; set; } = true;
    public int? LastActivatedAnnouncementId { get; private set; }
    public int? LastActivateOwnerUserId { get; private set; }
    public int? LastDeactivatedAnnouncementId { get; private set; }
    public int? LastDeactivateOwnerUserId { get; private set; }
    public int? LastAdministratorUserId { get; private set; }
    public Guid? LastAdminCorrelationId { get; private set; }
    public int? LastAdminDeactivatedAnnouncementId { get; private set; }
    public Car? AdminUpdatedCar { get; private set; }
    public Announcement? AdminUpdatedAnnouncement { get; private set; }
    public DateTime? LastAdminExpectedUpdatedAt { get; private set; }
    public bool AdminUpdateSucceeds { get; set; } = true;
    public bool AdminDeactivateSucceeds { get; set; } = true;

    public Dictionary<int, AnnouncementDetailsDto> Details { get; } = [];
    public List<AnnouncementListItemDto> ActiveAnnouncements { get; } = [];
    public List<AnnouncementListItemDto> UserAnnouncements { get; } = [];
    public List<AdminAnnouncementDto> AdminAnnouncements { get; } = [];

    public Task<int> CreateAsync(Car car, Announcement announcement)
    {
        CreateCallCount++;
        CreatedCar = car;
        CreatedAnnouncement = announcement;

        return Task.FromResult(AnnouncementIdToReturn);
    }

    public Task<AnnouncementDetailsDto?> GetActiveByIdAsync(int announcementId)
    {
        GetActiveByIdCallCount++;
        LastRequestedAnnouncementId = announcementId;

        Details.TryGetValue(announcementId, out var announcement);

        return Task.FromResult(announcement?.IsActive == true && !InactiveSellerUserIds.Contains(announcement.SellerUserId) ? announcement : null);
    }

    public Task<bool> UpdateAsync(Car car, Announcement announcement, int actorUserId, bool isAdministrator, DateTime? expectedUpdatedAt)
    {
        UpdateCallCount++;
        UpdatedCar = car;
        UpdatedAnnouncement = announcement;
        LastUpdateActorUserId = actorUserId;
        LastUpdateWasAdministrator = isAdministrator;
        LastExpectedUpdatedAt = expectedUpdatedAt;
        return Task.FromResult(UpdateSucceeds);
    }

    public Task<IReadOnlyList<AnnouncementListItemDto>> GetByUserIdAsync(int userId)
    {
        GetByUserIdCallCount++;
        LastRequestedUserId = userId;

        return Task.FromResult<IReadOnlyList<AnnouncementListItemDto>>(UserAnnouncements);
    }

    public Task<bool> ActivateForOwnerAsync(int announcementId, int ownerUserId)
    {
        ActivateCallCount++;
        LastActivatedAnnouncementId = announcementId;
        LastActivateOwnerUserId = ownerUserId;

        return Task.FromResult(ActivateSucceeds);
    }

    public Task<bool> DeactivateForOwnerAsync(int announcementId, int ownerUserId)
    {
        DeactivateCallCount++;
        LastDeactivatedAnnouncementId = announcementId;
        LastDeactivateOwnerUserId = ownerUserId;

        return Task.FromResult(DeactivateSucceeds);
    }

    public Task<AnnouncementDetailsDto?> GetByIdAsync(int announcementId)
    {
        GetByIdCallCount++;
        LastRequestedAnnouncementId = announcementId;

        Details.TryGetValue(announcementId, out var announcement);

        return Task.FromResult(announcement is not null && InactiveSellerUserIds.Contains(announcement.SellerUserId) ? null : announcement);
    }

    public Task<AnnouncementDetailsDto?> GetByIdForAdministrationAsync(int announcementId)
    {
        GetByIdForAdministrationCallCount++;
        LastRequestedAnnouncementId = announcementId;
        Details.TryGetValue(announcementId, out var announcement);
        return Task.FromResult(announcement);
    }

    public Task<PagedResult<AnnouncementListItemDto>> SearchActiveAsync(AnnouncementSearchDto search)
    {
        SearchActiveCallCount++;
        LastSearch = search;

        return Task.FromResult(new PagedResult<AnnouncementListItemDto> { Items = ActiveAnnouncements, Page = search.Page, PageSize = search.PageSize, TotalItems = ActiveAnnouncements.Count });
    }

    public Task<PagedResult<AdminAnnouncementDto>> SearchForAdministrationAsync(AdminAnnouncementSearchDto search)
    {
        AdminSearchCallCount++;
        LastAdminSearch = search;

        return Task.FromResult(new PagedResult<AdminAnnouncementDto> { Items = AdminAnnouncements, Page = search.Page, PageSize = search.PageSize, TotalItems = AdminAnnouncements.Count });
    }

    public Task<bool> UpdateForAdministrationWithAuditAsync(Car car, Announcement announcement, int administratorUserId, Guid correlationId, DateTime? expectedUpdatedAt)
    {
        AdminUpdateCallCount++;
        AdminUpdatedCar = car;
        AdminUpdatedAnnouncement = announcement;
        LastAdministratorUserId = administratorUserId;
        LastAdminCorrelationId = correlationId;
        LastAdminExpectedUpdatedAt = expectedUpdatedAt;

        return Task.FromResult(AdminUpdateSucceeds);
    }

    public Task<bool> DeactivateForAdministrationWithAuditAsync(int announcementId, int administratorUserId, Guid correlationId)
    {
        AdminDeactivateCallCount++;
        LastAdminDeactivatedAnnouncementId = announcementId;
        LastAdministratorUserId = administratorUserId;
        LastAdminCorrelationId = correlationId;

        return Task.FromResult(AdminDeactivateSucceeds);
    }
}
