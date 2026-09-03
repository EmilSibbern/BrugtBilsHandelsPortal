using System.ComponentModel.DataAnnotations;
using BrugtBilsHandelsPortal.DTOs.Announcements;
using BrugtBilsHandelsPortal.Models.Entities;
using BrugtBilsHandelsPortal.Services.Implementations;
using BrugtBilsHandelsPortal.Tests.Fakes;
using Xunit;

namespace BrugtBilsHandelsPortal.Tests.Services;

public class AdminAnnouncementServiceTests
{
    [Fact]
    [Trait("UserStory", "US17")]
    public async Task SearchAsync_AsAdministrator_NormalizesAndReturnsAnnouncements()
    {
        var announcementRepository = new FakeAnnouncementRepository();
        announcementRepository.AdminAnnouncements.Add(new AdminAnnouncementDto { Id = 42, Title = "Volvo V70", OwnerUserId = 7, OwnerUsername = "anna" });
        var service = CreateService(announcementRepository);
        var search = new AdminAnnouncementSearchDto
        {
            Query = "  Volvo  ",
            OwnerQuery = "  anna  "
        };

        var result = await service.SearchAsync(search);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Page);
        Assert.Single(result.Page.Items);
        Assert.Equal("Volvo", search.Query);
        Assert.Equal("anna", search.OwnerQuery);
        Assert.Equal(1, announcementRepository.AdminSearchCallCount);
    }

    [Fact]
    [Trait("UserStory", "US17")]
    public async Task SearchAsync_WithInvalidPaging_IsRejectedBeforeRepository()
    {
        var announcementRepository = new FakeAnnouncementRepository();
        var service = CreateService(announcementRepository);

        var result = await service.SearchAsync(new AdminAnnouncementSearchDto { Page = 0 });

        Assert.False(result.Succeeded);
        Assert.NotNull(result.ErrorMessage);
        Assert.Equal(0, announcementRepository.AdminSearchCallCount);
    }

    [Fact]
    [Trait("UserStory", "US17")]
    public async Task SearchAsync_AsOrdinaryUser_IsRejectedBeforeRepository()
    {
        var announcementRepository = new FakeAnnouncementRepository();
        var service = CreateService(announcementRepository, isAdministrator: false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.SearchAsync(new AdminAnnouncementSearchDto()));

        Assert.Equal(0, announcementRepository.AdminSearchCallCount);
    }

    [Fact]
    [Trait("UserStory", "US17")]
    public async Task GetForEditAsync_AsAdministrator_MapsAnnouncement()
    {
        var announcementRepository = new FakeAnnouncementRepository();
        announcementRepository.Details[42] = CreateAnnouncementDetails();
        var service = CreateService(announcementRepository);

        var result = await service.GetForEditAsync(42);

        Assert.NotNull(result);
        Assert.Equal(42, result.AnnouncementId);
        Assert.Equal("Volvo V70", result.Title);
        Assert.True(result.CanManageImages);
    }

    [Fact]
    [Trait("UserStory", "US17")]
    public async Task UpdateAsync_AsAdministrator_NormalizesAndPassesAuditValues()
    {
        var announcementRepository = new FakeAnnouncementRepository();
        announcementRepository.Details[42] = CreateAnnouncementDetails();
        var service = CreateService(announcementRepository);
        var expectedUpdatedAt = new DateTime(2026, 8, 30, 12, 0, 0, DateTimeKind.Utc);
        var request = CreateValidUpdateRequest(expectedUpdatedAt);

        await service.UpdateAsync(request);

        Assert.Equal(1, announcementRepository.AdminUpdateCallCount);
        Assert.Equal(1, announcementRepository.LastAdministratorUserId);
        Assert.NotNull(announcementRepository.LastAdminCorrelationId);
        Assert.NotEqual(Guid.Empty, announcementRepository.LastAdminCorrelationId);
        Assert.Equal(expectedUpdatedAt, announcementRepository.LastAdminExpectedUpdatedAt);
        Assert.Equal("Volvo V70", announcementRepository.AdminUpdatedAnnouncement?.Title);
        Assert.Equal("Sort", announcementRepository.AdminUpdatedCar?.Color);
    }

    [Fact]
    [Trait("UserStory", "US17")]
    public async Task UpdateAsync_WithInvalidRequest_IsRejectedBeforeRepository()
    {
        var announcementRepository = new FakeAnnouncementRepository();
        var service = CreateService(announcementRepository);
        var request = CreateValidUpdateRequest();
        request.Title = "";

        await Assert.ThrowsAsync<ValidationException>(() => service.UpdateAsync(request));

        Assert.Equal(0, announcementRepository.AdminUpdateCallCount);
    }

    [Fact]
    [Trait("UserStory", "US17")]
    public async Task DeactivateAsync_AsAdministrator_PassesActorAndCorrelationId()
    {
        var announcementRepository = new FakeAnnouncementRepository();
        announcementRepository.Details[42] = CreateAnnouncementDetails();
        var service = CreateService(announcementRepository);

        await service.DeactivateAsync(42);

        Assert.Equal(1, announcementRepository.AdminDeactivateCallCount);
        Assert.Equal(42, announcementRepository.LastAdminDeactivatedAnnouncementId);
        Assert.Equal(1, announcementRepository.LastAdministratorUserId);
        Assert.NotNull(announcementRepository.LastAdminCorrelationId);
        Assert.NotEqual(Guid.Empty, announcementRepository.LastAdminCorrelationId);
    }

    [Fact]
    [Trait("UserStory", "US17")]
    public async Task DeactivateAsync_WhenAlreadyInactive_IsRejectedBeforeMutation()
    {
        var announcementRepository = new FakeAnnouncementRepository();
        var details = CreateAnnouncementDetails();
        details.IsActive = false;
        announcementRepository.Details[42] = details;
        var service = CreateService(announcementRepository);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeactivateAsync(42));

        Assert.Equal(0, announcementRepository.AdminDeactivateCallCount);
    }

    [Fact]
    [Trait("UserStory", "US17")]
    public async Task GetImagesAsync_AsAdministrator_ReturnsImagesWithPublicUrls()
    {
        var announcementRepository = new FakeAnnouncementRepository();
        announcementRepository.Details[42] = CreateAnnouncementDetails();
        var imageRepository = new FakeImageRepository();
        imageRepository.Images.Add(new AnnouncementImage { Id = 9, AnnouncementId = 42, FileName = "volvo.jpg", IsPrimary = true });
        var service = CreateService(announcementRepository, imageRepository: imageRepository);

        var result = await service.GetImagesAsync(42);

        var image = Assert.Single(result);
        Assert.Equal("/uploads/volvo.jpg", image.Url);
        Assert.True(image.IsPrimary);
        Assert.Equal(1, imageRepository.AdminGetCallCount);
    }

    [Fact]
    [Trait("UserStory", "US17")]
    public async Task DeleteImageAsync_AsAdministrator_DeletesMetadataAndPhysicalFile()
    {
        var announcementRepository = new FakeAnnouncementRepository();
        var imageRepository = new FakeImageRepository
        {
            AdminDeleteFileNameToReturn = "volvo.jpg"
        };
        var imageStorage = new FakeImageStorage();
        var service = CreateService(announcementRepository, imageRepository, imageStorage);

        var result = await service.DeleteImageAsync(42, 9);

        Assert.True(result);
        Assert.Equal(1, imageRepository.AdminDeleteCallCount);
        Assert.Equal(42, imageRepository.LastAnnouncementId);
        Assert.Equal(9, imageRepository.LastDeletedImageId);
        Assert.Equal(1, imageRepository.LastAdministratorUserId);
        Assert.NotNull(imageRepository.LastAdminCorrelationId);
        Assert.NotEqual(Guid.Empty, imageRepository.LastAdminCorrelationId);
        Assert.Equal(1, imageStorage.DeleteCallCount);
        Assert.Equal("volvo.jpg", imageStorage.LastDeletedFileName);
    }

    [Fact]
    [Trait("UserStory", "US17")]
    public async Task DeleteImageAsync_WhenMetadataIsNotFound_DoesNotDeletePhysicalFile()
    {
        var announcementRepository = new FakeAnnouncementRepository();
        var imageRepository = new FakeImageRepository();
        var imageStorage = new FakeImageStorage();
        var service = CreateService(announcementRepository, imageRepository, imageStorage);

        var result = await service.DeleteImageAsync(42, 9);

        Assert.False(result);
        Assert.Equal(1, imageRepository.AdminDeleteCallCount);
        Assert.Equal(0, imageStorage.DeleteCallCount);
    }

    [Theory]
    [InlineData("edit")]
    [InlineData("update")]
    [InlineData("deactivate")]
    [InlineData("images")]
    [Trait("UserStory", "US17")]
    public async Task AdminOperations_WithInactiveSeller_UsePrivilegedLookup(string operation)
    {
        var repository = new FakeAnnouncementRepository();
        repository.Details[42] = CreateAnnouncementDetails();
        repository.InactiveSellerUserIds.Add(7);
        var images = new FakeImageRepository();
        images.Images.Add(new AnnouncementImage { Id = 9, AnnouncementId = 42, FileName = "volvo.jpg" });
        var service = CreateService(repository, images);

        switch (operation)
        {
            case "edit":
                Assert.NotNull(await service.GetForEditAsync(42));
                break;
            case "update":
                await service.UpdateAsync(CreateValidUpdateRequest());
                Assert.Equal(1, repository.AdminUpdateCallCount);
                break;
            case "deactivate":
                await service.DeactivateAsync(42);
                Assert.Equal(1, repository.AdminDeactivateCallCount);
                break;
            case "images":
                Assert.Single(await service.GetImagesAsync(42));
                Assert.Equal(1, images.AdminGetCallCount);
                break;
        }

        Assert.Equal(1, repository.GetByIdForAdministrationCallCount);
        Assert.Equal(0, repository.GetByIdCallCount);
    }

    private static AdminAnnouncementService CreateService(FakeAnnouncementRepository announcementRepository, FakeImageRepository? imageRepository = null, FakeImageStorage? imageStorage = null, bool isAdministrator = true)
    {
        var carBrandRepository = new FakeCarBrandRepository();
        carBrandRepository.Brands.Add(new CarBrand { BrandId = 2, BrandName = "Volvo" });

        return new AdminAnnouncementService(announcementRepository, imageRepository ?? new FakeImageRepository(), carBrandRepository, new FakeCurrentUserService { UserId = 1, IsAdministrator = isAdministrator }, imageStorage ?? new FakeImageStorage());
    }

    private static AnnouncementDetailsDto CreateAnnouncementDetails()
    {
        return new AnnouncementDetailsDto
        {
            Id = 42,
            Title = "Volvo V70",
            Description = "Velholdt familiebil",
            Price = 125_000,
            IsActive = true,
            SellerUserId = 7,
            SellerUsername = "anna",
            CarId = 8,
            BrandId = 2,
            BrandName = "Volvo",
            Model = "V70",
            ProductionYear = 2018,
            Mileage = 120_000,
            Color = "Sort"
        };
    }

    private static UpdateAnnouncementDto CreateValidUpdateRequest(DateTime? expectedUpdatedAt = null)
    {
        return new UpdateAnnouncementDto
        {
            AnnouncementId = 42,
            BrandId = 2,
            Model = " V70 ",
            ProductionYear = 2018,
            Mileage = 120_000,
            Color = " Sort ",
            Title = " Volvo V70 ",
            Description = " Velholdt familiebil ",
            Price = 125_000,
            ExpectedUpdatedAt = expectedUpdatedAt
        };
    }
}