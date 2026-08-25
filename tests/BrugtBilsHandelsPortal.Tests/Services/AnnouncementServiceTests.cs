using BrugtBilsHandelsPortal.DTOs.Announcements;
using BrugtBilsHandelsPortal.Models.Entities;
using BrugtBilsHandelsPortal.Services.Implementations;
using BrugtBilsHandelsPortal.Tests.Fakes;
using Xunit;

namespace BrugtBilsHandelsPortal.Tests.Services;

public class AnnouncementServiceTests
{
    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesCarAndAnnouncementForCurrentUser()
    {
        var announcementRepository = new FakeAnnouncementRepository
        {
            AnnouncementIdToReturn = 42
        };

        var carBrandRepository = CreateCarBrandRepository();
        var service = CreateService(
            announcementRepository,
            carBrandRepository,
            userId: 7);

        var result = await service.CreateAsync(
            CreateValidRequest());

        Assert.True(result.Succeeded);
        Assert.Equal(42, result.AnnouncementId);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(1, announcementRepository.CreateCallCount);

        var car = Assert.IsType<Car>(
            announcementRepository.CreatedCar);

        Assert.Equal(1, car.BrandId);
        Assert.Equal("Octavia", car.Model);
        Assert.Equal(2020, car.ProductionYear);
        Assert.Equal(85_000, car.Mileage);
        Assert.Equal("Sort", car.Color);

        var announcement = Assert.IsType<Announcement>(
            announcementRepository.CreatedAnnouncement);

        Assert.Equal(7, announcement.UserId);
        Assert.Equal("Skoda Octavia", announcement.Title);
        Assert.Equal("Velholdt familiebil.", announcement.Description);
        Assert.Equal(175_000, announcement.Price);
    }

    [Fact]
    public async Task CreateAsync_TrimsTextValuesAndConvertsBlankColorToNull()
    {
        var announcementRepository = new FakeAnnouncementRepository();
        var request = CreateValidRequest();

        request.Model = "  Octavia  ";
        request.Color = "   ";
        request.Title = "  Skoda Octavia  ";
        request.Description = "  Velholdt familiebil.  ";

        var service = CreateService(
            announcementRepository,
            CreateCarBrandRepository(),
            userId: 7);

        var result = await service.CreateAsync(request);

        Assert.True(result.Succeeded);
        Assert.Equal("Octavia", announcementRepository.CreatedCar?.Model);
        Assert.Null(announcementRepository.CreatedCar?.Color);
        Assert.Equal(
            "Skoda Octavia",
            announcementRepository.CreatedAnnouncement?.Title);
        Assert.Equal(
            "Velholdt familiebil.",
            announcementRepository.CreatedAnnouncement?.Description);
    }

    [Fact]
    public async Task CreateAsync_WithoutAuthentication_IsRejectedBeforeRepositoryCall()
    {
        var announcementRepository = new FakeAnnouncementRepository();
        var carBrandRepository = CreateCarBrandRepository();

        var service = CreateService(
            announcementRepository,
            carBrandRepository,
            userId: null);

        var result = await service.CreateAsync(
            CreateValidRequest());

        Assert.False(result.Succeeded);
        Assert.Contains("logget ind", result.ErrorMessage);
        Assert.Equal(0, carBrandRepository.ExistsCallCount);
        Assert.Equal(0, announcementRepository.CreateCallCount);
    }

    [Fact]
    public async Task CreateAsync_WithUnknownBrand_IsRejected()
    {
        var announcementRepository = new FakeAnnouncementRepository();
        var request = CreateValidRequest();
        request.BrandId = 999;

        var service = CreateService(
            announcementRepository,
            CreateCarBrandRepository(),
            userId: 7);

        var result = await service.CreateAsync(request);

        Assert.False(result.Succeeded);
        Assert.Contains("bilmærke", result.ErrorMessage);
        Assert.Equal(0, announcementRepository.CreateCallCount);
    }

    [Fact]
    public async Task CreateAsync_WithFutureProductionYear_IsRejected()
    {
        var announcementRepository = new FakeAnnouncementRepository();
        var carBrandRepository = CreateCarBrandRepository();
        var request = CreateValidRequest();

        request.ProductionYear = DateTime.UtcNow.Year + 2;

        var service = CreateService(
            announcementRepository,
            carBrandRepository,
            userId: 7);

        var result = await service.CreateAsync(request);

        Assert.False(result.Succeeded);
        Assert.Contains("fremtiden", result.ErrorMessage);
        Assert.Equal(0, carBrandRepository.ExistsCallCount);
        Assert.Equal(0, announcementRepository.CreateCallCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreateAsync_WithNonPositivePrice_IsRejected(
        int price)
    {
        var announcementRepository = new FakeAnnouncementRepository();
        var carBrandRepository = CreateCarBrandRepository();
        var request = CreateValidRequest();

        request.Price = price;

        var service = CreateService(
            announcementRepository,
            carBrandRepository,
            userId: 7);

        var result = await service.CreateAsync(request);

        Assert.False(result.Succeeded);
        Assert.Contains("mindst 1 kr", result.ErrorMessage);
        Assert.Equal(0, carBrandRepository.ExistsCallCount);
        Assert.Equal(0, announcementRepository.CreateCallCount);
    }

    [Fact]
    public async Task CreateAsync_WithMissingRequiredValues_IsRejectedBeforeRepositoryCalls()
    {
        var announcementRepository = new FakeAnnouncementRepository();
        var carBrandRepository = CreateCarBrandRepository();
        var request = CreateValidRequest();

        request.Model = string.Empty;

        var service = CreateService(
            announcementRepository,
            carBrandRepository,
            userId: 7);

        var result = await service.CreateAsync(request);

        Assert.False(result.Succeeded);
        Assert.Contains("Model", result.ErrorMessage);
        Assert.Equal(0, carBrandRepository.ExistsCallCount);
        Assert.Equal(0, announcementRepository.CreateCallCount);
    }

    [Fact]
    public async Task GetCarBrandsAsync_ReturnsBrandsFromRepository()
    {
        var carBrandRepository = CreateCarBrandRepository();

        var service = CreateService(
            new FakeAnnouncementRepository(),
            carBrandRepository,
            userId: 7);

        var brands = await service.GetCarBrandsAsync();

        var brand = Assert.Single(brands);
        Assert.Equal(1, brand.BrandId);
        Assert.Equal("Skoda", brand.BrandName);
    }

    [Fact]
    public async Task GetDetailsAsync_WithExistingAnnouncement_ReturnsDetails()
    {
        var announcementRepository =
            new FakeAnnouncementRepository();

        announcementRepository.Details[42] =
            CreateAnnouncementDetails(id: 42);

        var service = CreateService(
            announcementRepository,
            CreateCarBrandRepository(),
            userId: null);

        var result = await service.GetDetailsAsync(42);

        Assert.NotNull(result);
        Assert.Equal(42, result.Id);
        Assert.Equal("Skoda Octavia", result.Title);
        Assert.Equal("sælger", result.SellerUsername);
        Assert.Equal(
            42,
            announcementRepository.LastRequestedAnnouncementId);
        Assert.Equal(1, announcementRepository.GetByIdCallCount);
    }

    [Fact]
    public async Task GetDetailsAsync_WithMissingAnnouncement_ReturnsNull()
    {
        var announcementRepository =
            new FakeAnnouncementRepository();

        var service = CreateService(
            announcementRepository,
            CreateCarBrandRepository(),
            userId: null);

        var result = await service.GetDetailsAsync(999);

        Assert.Null(result);
        Assert.Equal(
            999,
            announcementRepository.LastRequestedAnnouncementId);
        Assert.Equal(1, announcementRepository.GetByIdCallCount);
    }

    [Fact]
    public async Task GetDetailsAsync_InactiveAnnouncementAsGuest_ReturnsNull()
    {
        var repository = new FakeAnnouncementRepository();
        repository.Details[42] = CreateAnnouncementDetails(
            id: 42,
            isActive: false);

        var service = CreateService(
            repository,
            CreateCarBrandRepository(),
            userId: null);

        var result = await service.GetDetailsAsync(42);

        Assert.Null(result);
        Assert.Equal(1, repository.GetByIdCallCount);
    }

    [Fact]
    public async Task GetDetailsAsync_InactiveAnnouncementAsDifferentUser_ReturnsNull()
    {
        var repository = new FakeAnnouncementRepository();
        repository.Details[42] = CreateAnnouncementDetails(
            id: 42,
            isActive: false);

        var service = CreateService(
            repository,
            CreateCarBrandRepository(),
            userId: 99);

        var result = await service.GetDetailsAsync(42);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetDetailsAsync_InactiveAnnouncementAsOwner_ReturnsDetails()
    {
        var repository = new FakeAnnouncementRepository();
        repository.Details[42] = CreateAnnouncementDetails(
            id: 42,
            isActive: false);

        var service = CreateService(
            repository,
            CreateCarBrandRepository(),
            userId: 7);

        var result = await service.GetDetailsAsync(42);

        Assert.NotNull(result);
        Assert.False(result.IsActive);
        Assert.Equal(7, result.SellerUserId);
    }

    [Fact]
    public async Task GetDetailsAsync_InactiveAnnouncementAsAdministrator_ReturnsDetails()
    {
        var repository = new FakeAnnouncementRepository();
        repository.Details[42] = CreateAnnouncementDetails(
            id: 42,
            isActive: false);

        var service = CreateService(
            repository,
            CreateCarBrandRepository(),
            userId: 1,
            isAdministrator: true);

        var result = await service.GetDetailsAsync(42);

        Assert.NotNull(result);
        Assert.False(result.IsActive);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetDetailsAsync_WithInvalidId_DoesNotCallRepository(
        int announcementId)
    {
        var announcementRepository =
            new FakeAnnouncementRepository();

        var service = CreateService(
            announcementRepository,
            CreateCarBrandRepository(),
            userId: null);

        var result =
            await service.GetDetailsAsync(announcementId);

        Assert.Null(result);
        Assert.Equal(0, announcementRepository.GetByIdCallCount);
        Assert.Null(
            announcementRepository.LastRequestedAnnouncementId);
    }

    [Fact]
    public async Task SearchActiveAsync_WithAnnouncements_ReturnsRepositoryPage()
    {
        var announcementRepository =
            new FakeAnnouncementRepository();

        announcementRepository.ActiveAnnouncements.Add(
            CreateAnnouncementListItem(
                id: 42,
                title: "Skoda Octavia"));

        announcementRepository.ActiveAnnouncements.Add(
            CreateAnnouncementListItem(
                id: 43,
                title: "Volvo V60"));

        var service = CreateService(
            announcementRepository,
            CreateCarBrandRepository(),
            userId: null);

        var result = await service.SearchActiveAsync(
            new AnnouncementSearchDto
            {
                Query = "  Octavia  ",
                BrandId = 1,
                MinPrice = 100_000,
                MaxPrice = 200_000,
                Sort = "price-asc",
                Page = 2,
                PageSize = 12
            });

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Page);
        Assert.Equal(2, result.Page.Items.Count);
        Assert.Equal(42, result.Page.Items[0].Id);
        Assert.Equal("Skoda Octavia", result.Page.Items[0].Title);
        Assert.Equal(43, result.Page.Items[1].Id);
        Assert.Equal("Volvo V60", result.Page.Items[1].Title);
        Assert.Equal(2, result.Page.Page);
        Assert.Equal(2, result.Page.TotalItems);
        Assert.Equal(1, announcementRepository.SearchActiveCallCount);

        Assert.NotNull(announcementRepository.LastSearch);
        Assert.Equal("Octavia", announcementRepository.LastSearch.Query);
        Assert.Equal(1, announcementRepository.LastSearch.BrandId);
        Assert.Equal(100_000, announcementRepository.LastSearch.MinPrice);
        Assert.Equal(200_000, announcementRepository.LastSearch.MaxPrice);
        Assert.Equal("price-asc", announcementRepository.LastSearch.Sort);
    }

    [Fact]
    public async Task SearchActiveAsync_WithoutAnnouncements_ReturnsEmptyPage()
    {
        var announcementRepository =
            new FakeAnnouncementRepository();

        var service = CreateService(
            announcementRepository,
            CreateCarBrandRepository(),
            userId: null);

        var result = await service.SearchActiveAsync(
            new AnnouncementSearchDto());

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Page);
        Assert.Empty(result.Page.Items);
        Assert.Equal(0, result.Page.TotalItems);
        Assert.Equal(1, announcementRepository.SearchActiveCallCount);
    }

    [Fact]
    public async Task SearchActiveAsync_WithInvalidPriceRange_IsRejectedBeforeRepositoryCall()
    {
        var announcementRepository =
            new FakeAnnouncementRepository();

        var service = CreateService(
            announcementRepository,
            CreateCarBrandRepository(),
            userId: null);

        var result = await service.SearchActiveAsync(
            new AnnouncementSearchDto
            {
                MinPrice = 200_000,
                MaxPrice = 100_000
            });

        Assert.False(result.Succeeded);
        Assert.Null(result.Page);
        Assert.Contains("Minimumsprisen", result.ErrorMessage);
        Assert.Equal(0, announcementRepository.SearchActiveCallCount);
    }

    [Fact]
    public async Task SearchActiveAsync_WithUnknownSort_UsesNewestSort()
    {
        var announcementRepository =
            new FakeAnnouncementRepository();

        var service = CreateService(
            announcementRepository,
            CreateCarBrandRepository(),
            userId: null);

        var result = await service.SearchActiveAsync(
            new AnnouncementSearchDto
            {
                Sort = "DROP TABLE Announcement"
            });

        Assert.True(result.Succeeded);
        Assert.NotNull(announcementRepository.LastSearch);
        Assert.Equal("newest", announcementRepository.LastSearch.Sort);
        Assert.Equal(1, announcementRepository.SearchActiveCallCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task SearchActiveAsync_WithInvalidPage_IsRejected(
        int page)
    {
        var announcementRepository =
            new FakeAnnouncementRepository();

        var service = CreateService(
            announcementRepository,
            CreateCarBrandRepository(),
            userId: null);

        var result = await service.SearchActiveAsync(
            new AnnouncementSearchDto
            {
                Page = page
            });

        Assert.False(result.Succeeded);
        Assert.Equal(0, announcementRepository.SearchActiveCallCount);
    }

    private static AnnouncementService CreateService(
        FakeAnnouncementRepository announcementRepository,
        FakeCarBrandRepository carBrandRepository,
        int? userId,
        bool isAdministrator = false)
    {
        return new AnnouncementService(
            announcementRepository,
            carBrandRepository,
            new FakeCurrentUserService
            {
                UserId = userId,
                IsAdministrator = isAdministrator
            });
    }

    private static FakeCarBrandRepository CreateCarBrandRepository()
    {
        var repository = new FakeCarBrandRepository();

        repository.Brands.Add(
            new CarBrand
            {
                BrandId = 1,
                BrandName = "Skoda"
            });

        return repository;
    }

    private static CreateAnnouncementDto CreateValidRequest()
    {
        return new CreateAnnouncementDto
        {
            BrandId = 1,
            Model = "Octavia",
            ProductionYear = 2020,
            Mileage = 85_000,
            Color = "Sort",
            Title = "Skoda Octavia",
            Description = "Velholdt familiebil.",
            Price = 175_000
        };
    }

    private static AnnouncementDetailsDto CreateAnnouncementDetails(
        int id,
        bool isActive = true)
    {
        return new AnnouncementDetailsDto
        {
            Id = id,
            Title = "Skoda Octavia",
            Description = "Velholdt familiebil.",
            Price = 175_000,
            CreatedAt = DateTime.UtcNow,
            IsActive = isActive,
            SellerUserId = 7,
            SellerUsername = "sælger",
            CarId = 10,
            BrandId = 1,
            BrandName = "Skoda",
            Model = "Octavia",
            ProductionYear = 2020,
            Mileage = 85_000,
            Color = "Sort"
        };
    }

    private static AnnouncementListItemDto CreateAnnouncementListItem(
        int id,
        string title)
    {
        return new AnnouncementListItemDto
        {
            Id = id,
            Title = title,
            Price = 175_000,
            CreatedAt = DateTime.UtcNow,
            BrandName = "Skoda",
            Model = "Octavia",
            ProductionYear = 2020,
            Mileage = 85_000
        };
    }
}
