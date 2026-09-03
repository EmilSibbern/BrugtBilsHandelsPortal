using System.Reflection;
using BrugtBilsHandelsPortal.Components.Pages.Admin;
using BrugtBilsHandelsPortal.DTOs.Announcements;
using BrugtBilsHandelsPortal.DTOs.Common;
using BrugtBilsHandelsPortal.DTOs.Images;
using BrugtBilsHandelsPortal.Models.Entities;
using BrugtBilsHandelsPortal.Services.Interfaces;
using Bunit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BrugtBilsHandelsPortal.Tests.Components;

public class AdminAnnouncementComponentsTests
{
    [Fact]
    [Trait("UserStory", "US17")]
    public void AdminPages_RequireAdministratorRole()
    {
        var pageTypes = new[]
        {
            typeof(ManageAnnouncements),
            typeof(AdminEditAnnouncement),
            typeof(AdminAnnouncementImages)
        };

        foreach (var pageType in pageTypes)
        {
            var attribute = Assert.Single(pageType.GetCustomAttributes<AuthorizeAttribute>());

            Assert.Equal("Administrator", attribute.Roles);
        }
    }

    [Fact]
    [Trait("UserStory", "US17")]
    public void Overview_ShowsAnnouncementPreviewAndOwner()
    {
        var service = CreateAdminService();
        service.Announcements.Add(CreateAdminAnnouncement());
        using var context = CreateContext(service);

        var cut = context.Render<ManageAnnouncements>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Volvo V70", cut.Markup);
            Assert.Contains("anna", cut.Markup);
            Assert.Contains("/uploads/volvo.jpg", cut.Markup);
            Assert.Contains("3", cut.Markup);
        });
    }

    [Fact]
    [Trait("UserStory", "US17")]
    public void Overview_SearchForm_SendsQueryOwnerAndStatus()
    {
        var service = CreateAdminService();
        using var context = CreateContext(service);
        var cut = context.Render<ManageAnnouncements>();

        cut.WaitForElement("#announcement-query").Change("Volvo");
        cut.Find("#owner-query").Change("anna");
        cut.Find("#status-filter").Change("true");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(2, service.SearchCallCount);
            Assert.NotNull(service.LastSearch);
            Assert.Equal("Volvo", service.LastSearch.Query);
            Assert.Equal("anna", service.LastSearch.OwnerQuery);
            Assert.True(service.LastSearch.IsActive);
            Assert.Equal(1, service.LastSearch.Page);
        });
    }

    [Fact]
    [Trait("UserStory", "US17")]
    public void Overview_DeactivationRequiresConfirmationAndCallsService()
    {
        var service = CreateAdminService();
        service.Announcements.Add(CreateAdminAnnouncement());
        using var context = CreateContext(service);
        var cut = context.Render<ManageAnnouncements>();

        var openButton = cut.WaitForElement("button.btn-outline-danger");
        openButton.Click();

        Assert.Contains("Er du sikker", cut.Markup);
        Assert.Equal(0, service.DeactivateCallCount);

        var confirmButton = cut.FindAll("button").Single(button => button.TextContent.Trim() == "Deaktiver annonce" && button.ClassList.Contains("btn-danger"));
        confirmButton.Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(1, service.DeactivateCallCount);
            Assert.Equal(42, service.LastDeactivatedAnnouncementId);
            Assert.Contains("Annoncen er deaktiveret", cut.Markup);
        });
    }

    [Fact]
    [Trait("UserStory", "US17")]
    public void EditPage_PrefillsAndSubmitsAnnouncement()
    {
        var adminService = CreateAdminService();
        adminService.EditModel = CreateUpdateRequest();
        using var context = CreateContext(adminService);
        context.Services.AddSingleton<IAnnouncementService>(new AnnouncementServiceStub());

        var cut = context.Render<AdminEditAnnouncement>(parameters => parameters.Add(page => page.Id, 42));

        cut.WaitForAssertion(() => Assert.Equal("Volvo V70", cut.Find("#title").GetAttribute("value")));

        cut.Find("#title").Change("Opdateret Volvo V70");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(1, adminService.UpdateCallCount);
            Assert.Equal("Opdateret Volvo V70", adminService.LastUpdateRequest?.Title);
            Assert.EndsWith("/admin/announcements", context.Services.GetRequiredService<NavigationManager>().Uri);
        });
    }

    [Fact]
    [Trait("UserStory", "US17")]
    public void ImagePage_RequiresConfirmationAndDeletesSelectedImage()
    {
        var service = CreateAdminService();
        service.EditModel = CreateUpdateRequest();
        service.Images.Add(new AnnouncementImageDto { Id = 9, AnnouncementId = 42, FileName = "volvo.jpg", Url = "/uploads/volvo.jpg", IsPrimary = true });
        using var context = CreateContext(service);

        var cut = context.Render<AdminAnnouncementImages>(parameters => parameters.Add(page => page.Id, 42));

        var openButton = cut.WaitForElement("button.btn-outline-danger");
        openButton.Click();

        Assert.Contains("Handlingen kan ikke fortrydes", cut.Markup);
        Assert.Equal(0, service.DeleteImageCallCount);

        var confirmButton = cut.FindAll("button").Single(button => button.TextContent.Trim() == "Fjern billede" && button.ClassList.Contains("btn-danger"));
        confirmButton.Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(1, service.DeleteImageCallCount);
            Assert.Equal(42, service.LastImageAnnouncementId);
            Assert.Equal(9, service.LastDeletedImageId);
            Assert.Contains("Billedet er fjernet", cut.Markup);
            Assert.Contains("Annoncen har ingen billeder", cut.Markup);
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("UserStory", "US17")]
    public void EditPage_WhenLoadingFails_ShowsSingleErrorWithoutForm(bool unauthorized)
    {
        var service = CreateAdminService();
        service.LoadException = unauthorized ? new UnauthorizedAccessException() : new InvalidOperationException("Internal database details");
        using var context = CreateContext(service);
        context.Services.AddSingleton<IAnnouncementService>(new AnnouncementServiceStub());

        var cut = context.Render<AdminEditAnnouncement>(parameters => parameters.Add(page => page.Id, 42));

        cut.WaitForAssertion(() =>
        {
            var error = Assert.Single(cut.FindAll(".alert-danger"));
            Assert.Contains(unauthorized ? "Du har ikke adgang" : "Annoncen kunne ikke indlæses", error.TextContent);
            Assert.Empty(cut.FindAll("form"));
            Assert.DoesNotContain("Internal database details", cut.Markup);
        });
    }

    [Fact]
    [Trait("UserStory", "US17")]
    public void EditPage_WhenSavingFails_ShowsOnlyOneErrorAndPreservesInput()
    {
        var service = CreateAdminService();
        service.EditModel = CreateUpdateRequest();
        service.UpdateException = new InvalidOperationException("Genindlæs siden og prøv igen.");
        using var context = CreateContext(service);
        context.Services.AddSingleton<IAnnouncementService>(new AnnouncementServiceStub());
        var originalUri = context.Services.GetRequiredService<NavigationManager>().Uri;
        var cut = context.Render<AdminEditAnnouncement>(parameters => parameters.Add(page => page.Id, 42));

        cut.WaitForElement("#title").Change("Bevar denne titel");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
        {
            var error = Assert.Single(cut.FindAll(".alert-danger"));
            Assert.Contains("Genindlæs siden", error.TextContent);
            Assert.Equal(1, service.UpdateCallCount);
            Assert.Equal("Bevar denne titel", cut.Find("#title").GetAttribute("value"));
            Assert.Equal(originalUri, context.Services.GetRequiredService<NavigationManager>().Uri);
        });
    }

    private static BunitContext CreateContext(AdminAnnouncementServiceStub service)
    {
        var context = new BunitContext();
        context.Services.AddSingleton<IAdminAnnouncementService>(service);
        return context;
    }

    private static AdminAnnouncementServiceStub CreateAdminService()
    {
        return new AdminAnnouncementServiceStub();
    }

    private static AdminAnnouncementDto CreateAdminAnnouncement()
    {
        return new AdminAnnouncementDto
        {
            Id = 42,
            Title = "Volvo V70",
            Price = 125_000,
            IsActive = true,
            CreatedAt = new DateTime(2026, 8, 30),
            OwnerUserId = 7,
            OwnerUsername = "anna",
            BrandName = "Volvo",
            Model = "V70",
            ProductionYear = 2018,
            PrimaryImageFileName = "volvo.jpg",
            ImageCount = 3
        };
    }

    private static UpdateAnnouncementDto CreateUpdateRequest()
    {
        return new UpdateAnnouncementDto
        {
            AnnouncementId = 42,
            BrandId = 2,
            Model = "V70",
            ProductionYear = 2018,
            Mileage = 120_000,
            Color = "Sort",
            Title = "Volvo V70",
            Description = "Velholdt familiebil",
            Price = 125_000,
            CanManageImages = true
        };
    }

    private sealed class AdminAnnouncementServiceStub : IAdminAnnouncementService
    {
        public List<AdminAnnouncementDto> Announcements { get; } = [];
        public List<AnnouncementImageDto> Images { get; } = [];
        public UpdateAnnouncementDto? EditModel { get; set; }
        public Exception? LoadException { get; set; }
        public Exception? UpdateException { get; set; }
        public int SearchCallCount { get; private set; }
        public int UpdateCallCount { get; private set; }
        public int DeactivateCallCount { get; private set; }
        public int DeleteImageCallCount { get; private set; }
        public AdminAnnouncementSearchDto? LastSearch { get; private set; }
        public UpdateAnnouncementDto? LastUpdateRequest { get; private set; }
        public int? LastDeactivatedAnnouncementId { get; private set; }
        public int? LastImageAnnouncementId { get; private set; }
        public int? LastDeletedImageId { get; private set; }

        public Task<AdminAnnouncementSearchResult> SearchAsync(AdminAnnouncementSearchDto search)
        {
            SearchCallCount++;
            LastSearch = search;

            return Task.FromResult(AdminAnnouncementSearchResult.Successful(new PagedResult<AdminAnnouncementDto> { Items = Announcements, Page = search.Page, PageSize = search.PageSize, TotalItems = Announcements.Count }));
        }

        public Task<UpdateAnnouncementDto?> GetForEditAsync(int announcementId)
        {
            if (LoadException is not null)
            {
                return Task.FromException<UpdateAnnouncementDto?>(LoadException);
            }
            return Task.FromResult(EditModel);
        }

        public Task UpdateAsync(UpdateAnnouncementDto request)
        {
            UpdateCallCount++;
            LastUpdateRequest = request;
            if (UpdateException is not null)
            {
                return Task.FromException(UpdateException);
            }
            return Task.CompletedTask;
        }

        public Task DeactivateAsync(int announcementId)
        {
            DeactivateCallCount++;
            LastDeactivatedAnnouncementId = announcementId;

            var announcement = Announcements.FirstOrDefault(item => item.Id == announcementId);
            if (announcement is not null)
            {
                announcement.IsActive = false;
            }

            return Task.CompletedTask;
        }

        public Task<bool> DeleteImageAsync(int announcementId, int imageId, CancellationToken cancellationToken = default)
        {
            DeleteImageCallCount++;
            LastImageAnnouncementId = announcementId;
            LastDeletedImageId = imageId;
            Images.RemoveAll(image => image.Id == imageId);
            return Task.FromResult(true);
        }

        public Task<IReadOnlyList<AnnouncementImageDto>> GetImagesAsync(int announcementId)
        {
            return Task.FromResult<IReadOnlyList<AnnouncementImageDto>>(Images.ToArray());
        }
    }

    private sealed class AnnouncementServiceStub : IAnnouncementService
    {
        public Task<IReadOnlyList<CarBrand>> GetCarBrandsAsync()
        {
            IReadOnlyList<CarBrand> brands = [new CarBrand
                {
                    BrandId = 2,
                    BrandName = "Volvo"
                }
            ];

            return Task.FromResult(brands);
        }

        public Task<CreateAnnouncementResult> CreateAsync(CreateAnnouncementDto request) => throw new NotSupportedException();

        public Task<UpdateAnnouncementDto?> GetForEditAsync(int announcementId) => throw new NotSupportedException();

        public Task UpdateAsync(UpdateAnnouncementDto request) => throw new NotSupportedException();

        public Task<IReadOnlyList<AnnouncementListItemDto>> GetMineAsync() => throw new NotSupportedException();

        public Task ActivateAsync(int announcementId) => throw new NotSupportedException();

        public Task DeactivateAsync(int announcementId) => throw new NotSupportedException();

        public Task<AnnouncementDetailsDto?> GetDetailsAsync(int announcementId) => throw new NotSupportedException();

        public Task<AnnouncementSearchResult> SearchActiveAsync(AnnouncementSearchDto search) => throw new NotSupportedException();
    }
}