using BrugtBilsHandelsPortal.Components.Pages.Announcements;
using BrugtBilsHandelsPortal.Configuration;
using BrugtBilsHandelsPortal.DTOs.Announcements;
using BrugtBilsHandelsPortal.DTOs.Images;
using BrugtBilsHandelsPortal.Models.Entities;
using BrugtBilsHandelsPortal.Services.Interfaces;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace BrugtBilsHandelsPortal.Tests.Components;

public class EditAnnouncementTests
{
    [Fact]
    [Trait("UserStory", "US07")]
    public void ExistingAnnouncement_PrefillsFormFields()
    {
        using var context = CreateContext(new StubAnnouncementService());

        var cut = RenderPage(context);

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("1", cut.Find("#brand").GetAttribute("value"));
            Assert.Equal("Octavia", cut.Find("#model").GetAttribute("value"));
            Assert.Equal("2020", cut.Find("#year").GetAttribute("value"));
            Assert.Equal("85000", cut.Find("#mileage").GetAttribute("value"));
            Assert.Equal("Sort", cut.Find("#color").GetAttribute("value"));
            Assert.Equal("Skoda Octavia", cut.Find("#title").GetAttribute("value"));
            Assert.Equal("175000", cut.Find("#price").GetAttribute("value"));
            Assert.NotNull(cut.Find("#description"));
        });
    }

    [Fact]
    [Trait("UserStory", "US07")]
    public void ValidChanges_AreSubmittedAndNavigateToDetails()
    {
        var service = new StubAnnouncementService();
        using var context = CreateContext(service);
        var cut = RenderPage(context);

        cut.WaitForElement("#title").Change("Opdateret Skoda Octavia");
        cut.Find("[data-testid='save-announcement']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(service.LastUpdateRequest);
            Assert.Equal(42, service.LastUpdateRequest.AnnouncementId);
            Assert.Equal("Opdateret Skoda Octavia", service.LastUpdateRequest.Title);
            Assert.EndsWith("/announcements/42", context.Services.GetRequiredService<NavigationManager>().Uri);
        });
    }

    [Fact]
    [Trait("UserStory", "US07")]
    public void ForbiddenAnnouncement_ShowsAccessStateWithoutForm()
    {
        using var context = CreateContext(new StubAnnouncementService { LoadException = new UnauthorizedAccessException() });

        var cut = RenderPage(context);

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Du har ikke adgang til at redigere denne annonce.", cut.Find("[role='alert']").TextContent);
            Assert.Empty(cut.FindAll("form"));
        });
    }

    [Fact]
    [Trait("UserStory", "US07")]
    public void ConcurrencyConflict_ShowsHelpfulMessageAndKeepsForm()
    {
        using var context = CreateContext(new StubAnnouncementService { UpdateException = new InvalidOperationException("Annoncen er blevet ændret af en anden bruger. Genindlæs siden og prøv igen.") });
        var cut = RenderPage(context);

        cut.WaitForElement("[data-testid='save-announcement']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Annoncen er blevet ændret af en anden.", cut.Find("[role='alert']").TextContent);
            Assert.NotNull(cut.Find("form"));
        });
    }

    [Fact]
    [Trait("UserStory", "US07")]
    public void Owner_EditPage_ShowsImageManagementActions()
    {
        using var context = CreateContext(new StubAnnouncementService { CanManageImages = true });

        var cut = RenderPage(context);

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.Find("[data-testid='delete-image']"));
            Assert.Contains("Tilføj billeder", cut.Markup);
        });
    }

    private static BunitContext CreateContext(IAnnouncementService service)
    {
        var context = new BunitContext();
        context.Services.AddSingleton(service);
        context.Services.AddSingleton<IImageService>(new StubImageService());
        context.Services.AddSingleton<IOptions<ImageUploadOptions>>(Options.Create(new ImageUploadOptions { MaxFileSizeBytes = 5 * 1024 * 1024, MaxImagesPerAnnouncement = 10, UploadDirectory = "uploads" }));
        return context;
    }

    private static IRenderedComponent<EditAnnouncement> RenderPage(BunitContext context)
    {
        return context.Render<EditAnnouncement>(parameters => parameters.Add(component => component.Id, 42));
    }

    private sealed class StubAnnouncementService : IAnnouncementService
    {
        public Exception? LoadException { get; init; }
        public Exception? UpdateException { get; init; }
        public bool CanManageImages { get; init; }
        public UpdateAnnouncementDto? LastUpdateRequest { get; private set; }

        public Task<IReadOnlyList<CarBrand>> GetCarBrandsAsync() => Task.FromResult<IReadOnlyList<CarBrand>>([new CarBrand { BrandId = 1, BrandName = "Skoda" }]);

        public Task<UpdateAnnouncementDto?> GetForEditAsync(int announcementId)
        {
            if (LoadException is not null)
            {
                return Task.FromException<UpdateAnnouncementDto?>(LoadException);
            }

            return Task.FromResult<UpdateAnnouncementDto?>(new UpdateAnnouncementDto { AnnouncementId = announcementId, BrandId = 1, Model = "Octavia", ProductionYear = 2020, Mileage = 85_000, Color = "Sort", Title = "Skoda Octavia", Description = "Velholdt familiebil.", Price = 175_000, ExpectedUpdatedAt = new DateTime(2026, 8, 24, 12, 0, 0), CanManageImages = CanManageImages });
        }

        public Task UpdateAsync(UpdateAnnouncementDto request)
        {
            LastUpdateRequest = request;

            return UpdateException is null ? Task.CompletedTask : Task.FromException(UpdateException);
        }

        public Task DeactivateAsync(int announcementId) => throw new NotSupportedException();

        public Task<IReadOnlyList<AnnouncementListItemDto>> GetMineAsync() => throw new NotSupportedException();

        public Task ActivateAsync(int announcementId) => throw new NotSupportedException();

        public Task<CreateAnnouncementResult> CreateAsync(CreateAnnouncementDto request) => throw new NotSupportedException();

        public Task<AnnouncementDetailsDto?> GetDetailsAsync(int announcementId) => throw new NotSupportedException();

        public Task<AnnouncementSearchResult> SearchActiveAsync(AnnouncementSearchDto search) => throw new NotSupportedException();
    }

    private sealed class StubImageService : IImageService
    {
        public Task<IReadOnlyList<AnnouncementImageDto>> GetForActiveAnnouncementAsync(int announcementId) => Task.FromResult<IReadOnlyList<AnnouncementImageDto>>([new AnnouncementImageDto { Id = 1, AnnouncementId = announcementId, FileName = "octavia.jpg", Url = "/uploads/octavia.jpg", IsPrimary = true, SortOrder = 0 }]);

        public Task<ImageUploadResult> UploadAsync(UploadAnnouncementImageRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<bool> SetPrimaryAsync(int announcementId, int imageId) => throw new NotSupportedException();

        public Task<bool> DeleteAsync(int announcementId, int imageId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}