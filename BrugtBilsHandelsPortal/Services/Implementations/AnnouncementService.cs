using System.ComponentModel.DataAnnotations;
using BrugtBilsHandelsPortal.Data.Repositories.Interfaces;
using BrugtBilsHandelsPortal.DTOs.Announcements;
using BrugtBilsHandelsPortal.Models.Entities;
using BrugtBilsHandelsPortal.Security;
using BrugtBilsHandelsPortal.Services.Interfaces;

namespace BrugtBilsHandelsPortal.Services.Implementations;

/// <summary>Samler validering, autorisation og normalisering for annoncens use cases.</summary>
public class AnnouncementService : IAnnouncementService
{
    private readonly IAnnouncementRepository
        _announcementRepository;

    private readonly ICarBrandRepository
        _carBrandRepository;

    private readonly ICurrentUserService
        _currentUserService;

    public AnnouncementService(
        IAnnouncementRepository announcementRepository,
        ICarBrandRepository carBrandRepository,
        ICurrentUserService currentUserService)
    {
        _announcementRepository = announcementRepository;
        _carBrandRepository = carBrandRepository;
        _currentUserService = currentUserService;
    }

    public Task<IReadOnlyList<CarBrand>>
        GetCarBrandsAsync()
    {
        return _carBrandRepository.GetAllAsync();
    }

    public async Task<CreateAnnouncementResult> CreateAsync(
        CreateAnnouncementDto request)
    {
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            validationResults,
            validateAllProperties: true);

        if (!isValid)
        {
            return CreateAnnouncementResult.Failed(
                validationResults.First().ErrorMessage
                ?? "Annonceoplysningerne er ugyldige.");
        }

        var userId =
            await _currentUserService.GetUserIdAsync();

        if (userId is null)
        {
            return CreateAnnouncementResult.Failed(
                "Du skal være logget ind for at oprette en annonce.");
        }

        // UTC gør årstalskontrollen uafhængig af serverens lokale tidszone.
        var currentYear = DateTime.UtcNow.Year;

        if (request.ProductionYear > currentYear + 1)
        {
            return CreateAnnouncementResult.Failed(
                "Produktionsåret kan ikke ligge i fremtiden.");
        }

        var brandExists =
            await _carBrandRepository.ExistsAsync(
                request.BrandId);

        if (!brandExists)
        {
            return CreateAnnouncementResult.Failed(
                "Det valgte bilmærke findes ikke.");
        }

        var car = new Car
        {
            BrandId = request.BrandId,
            Model = request.Model.Trim(),
            ProductionYear =
                checked((short)request.ProductionYear),
            Mileage = request.Mileage,

            Color = string.IsNullOrWhiteSpace(request.Color)
                ? null
                : request.Color.Trim()
        };

        var announcement = new Announcement
        {
            UserId = userId.Value,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Price = request.Price
        };

        var announcementId =
            await _announcementRepository.CreateAsync(
                car,
                announcement);

        return CreateAnnouncementResult.Successful(
            announcementId);
    }

    public async Task<UpdateAnnouncementDto?> GetForEditAsync(
        int announcementId)
    {
        if (announcementId <= 0)
        {
            return null;
        }

        var currentUserId =
            await _currentUserService.GetUserIdAsync();

        if (currentUserId is null)
        {
            throw new UnauthorizedAccessException(
                "Du skal være logget ind for at redigere en annonce.");
        }

        var announcement =
            await _announcementRepository.GetByIdAsync(
                announcementId);

        if (announcement is null)
        {
            return null;
        }

        var isAdministrator =
            await _currentUserService.IsInRoleAsync(
                "Administrator");

        var isOwner =
            announcement.SellerUserId == currentUserId.Value;

        if (!isOwner && !isAdministrator)
        {
            throw new UnauthorizedAccessException(
                "Du har ikke adgang til at redigere annoncen.");
        }

        return new UpdateAnnouncementDto
        {
            AnnouncementId = announcement.Id,
            BrandId = announcement.BrandId,
            Model = announcement.Model,
            ProductionYear = announcement.ProductionYear,
            Mileage = announcement.Mileage,
            Color = announcement.Color,
            Title = announcement.Title,
            Description = announcement.Description,
            Price = announcement.Price,
            ExpectedUpdatedAt = announcement.UpdatedAt,
            CanManageImages = isOwner
        };
    }

    public async Task UpdateAsync(
        UpdateAnnouncementDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            validationResults,
            validateAllProperties: true);

        if (!isValid)
        {
            throw new ValidationException(
                validationResults.First().ErrorMessage
                ?? "Annonceoplysningerne er ugyldige.");
        }

        var currentUserId =
            await _currentUserService.GetUserIdAsync();

        if (currentUserId is null)
        {
            throw new UnauthorizedAccessException(
                "Du skal være logget ind for at redigere en annonce.");
        }

        if (request.ProductionYear > DateTime.UtcNow.Year + 1)
        {
            throw new ValidationException(
                "Produktionsåret kan ikke ligge i fremtiden.");
        }

        var brandExists =
            await _carBrandRepository.ExistsAsync(request.BrandId);

        if (!brandExists)
        {
            throw new ValidationException(
                "Det valgte bilmærke findes ikke.");
        }

        var existingAnnouncement =
            await _announcementRepository.GetByIdAsync(
                request.AnnouncementId);

        if (existingAnnouncement is null)
        {
            throw new InvalidOperationException(
                "Annoncen findes ikke.");
        }

        var isAdministrator =
            await _currentUserService.IsInRoleAsync(
                "Administrator");
        var isOwner =
            existingAnnouncement.SellerUserId == currentUserId.Value;

        if (!isOwner && !isAdministrator)
        {
            throw new UnauthorizedAccessException(
                "Du har ikke adgang til at redigere annoncen.");
        }

        var car = new Car
        {
            CarId = existingAnnouncement.CarId,
            BrandId = request.BrandId,
            Model = request.Model.Trim(),
            ProductionYear = checked((short)request.ProductionYear),
            Mileage = request.Mileage,
            Color = string.IsNullOrWhiteSpace(request.Color)
                ? null
                : request.Color.Trim()
        };

        var announcement = new Announcement
        {
            Id = existingAnnouncement.Id,
            UserId = existingAnnouncement.SellerUserId,
            CarId = existingAnnouncement.CarId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Price = request.Price,
            IsActive = existingAnnouncement.IsActive
        };

        var wasUpdated =
            await _announcementRepository.UpdateAsync(
                car,
                announcement,
                currentUserId.Value,
                isAdministrator,
                request.ExpectedUpdatedAt);

        if (!wasUpdated)
        {
            throw new InvalidOperationException(
                "Annoncen er blevet ændret af en anden bruger. Genindlæs siden og prøv igen.");
        }
    }

    public async Task<AnnouncementDetailsDto?> GetDetailsAsync(
        int announcementId)
    {
        if (announcementId <= 0)
        {
            return null;
        }

        var announcement =
            await _announcementRepository.GetByIdAsync(
                announcementId);

        if (announcement is null)
        {
            return null;
        }

        // Aktive annoncer er offentlige; inaktive annoncer er kun synlige for ejer og administrator.
        if (announcement.IsActive)
        {
            return announcement;
        }

        var currentUserId =
            await _currentUserService.GetUserIdAsync();

        var isOwner =
            currentUserId.HasValue
            && currentUserId.Value
            == announcement.SellerUserId;

        if (isOwner)
        {
            return announcement;
        }

        var isAdministrator =
            await _currentUserService.IsInRoleAsync(
                "Administrator");

        return isAdministrator
            ? announcement
            : null;
    }
    
    public async Task<AnnouncementSearchResult> SearchActiveAsync(
        AnnouncementSearchDto search)
    {
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            search,
            new ValidationContext(search),
            validationResults,
            validateAllProperties: true);

        if (!isValid)
        {
            return AnnouncementSearchResult.Failed(
                validationResults.First().ErrorMessage
                ?? "Søgekriterierne er ugyldige.");
        }

        if (search.MinPrice.HasValue
            && search.MaxPrice.HasValue
            && search.MinPrice > search.MaxPrice)
        {
            return AnnouncementSearchResult.Failed(
                "Minimumsprisen må ikke være højere end maksimumsprisen.");
        }

        // Sortering begrænses til kendte værdier, så repository-laget aldrig bygger SQL fra fri tekst.
        var allowedSortValues = new[]
        {
            "newest",
            "price-asc",
            "price-desc",
            "mileage-asc"
        };

        if (!allowedSortValues.Contains(search.Sort))
        {
            search.Sort = "newest";
        }

        search.Query =
            string.IsNullOrWhiteSpace(search.Query)
                ? null
                : search.Query.Trim();

        search.Page = Math.Max(1, search.Page);
        search.PageSize = Math.Clamp(
            search.PageSize,
            1,
            100);

        var page =
            await _announcementRepository.SearchActiveAsync(
                search);

        return AnnouncementSearchResult.Successful(page);
    }
}
