using System.ComponentModel.DataAnnotations;
using BrugtBilsHandelsPortal.Data.Repositories.Interfaces;
using BrugtBilsHandelsPortal.DTOs.Announcements;
using BrugtBilsHandelsPortal.DTOs.Images;
using BrugtBilsHandelsPortal.Models.Entities;
using BrugtBilsHandelsPortal.Security;
using BrugtBilsHandelsPortal.Services.Interfaces;
using BrugtBilsHandelsPortal.Storage;

namespace BrugtBilsHandelsPortal.Services.Implementations;

/// <summary>
/// Håndhæver administratoradgang og koordinerer
/// moderation af annoncer og billeder.
/// </summary>
public class AdminAnnouncementService : IAdminAnnouncementService
{
    private readonly IAnnouncementRepository _announcementRepository;

    private readonly IImageRepository _imageRepository;

    private readonly ICarBrandRepository _carBrandRepository;

    private readonly ICurrentUserService _currentUserService;

    private readonly IImageStorage _imageStorage;

    public AdminAnnouncementService(IAnnouncementRepository announcementRepository, IImageRepository imageRepository, ICarBrandRepository carBrandRepository, ICurrentUserService currentUserService, IImageStorage imageStorage)
    {
        _announcementRepository = announcementRepository;

        _imageRepository = imageRepository;

        _carBrandRepository = carBrandRepository;

        _currentUserService = currentUserService;

        _imageStorage = imageStorage;
    }

    public async Task<AdminAnnouncementSearchResult> SearchAsync(AdminAnnouncementSearchDto search)
    {
        ArgumentNullException.ThrowIfNull(search);

        // Autorisation udføres før opslaget, så almindelige
        // brugere ikke kan hente administrative data.
        await GetAdministratorUserIdAsync();

        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(search, new ValidationContext(search), validationResults, validateAllProperties: true);

        if (!isValid)
        {
            return AdminAnnouncementSearchResult.Failed(validationResults.First().ErrorMessage ?? "Søgekriterierne er ugyldige.");
        }

        search.Query = NormalizeOptionalText(search.Query);

        search.OwnerQuery = NormalizeOptionalText(search.OwnerQuery);

        var page = await _announcementRepository.SearchForAdministrationAsync(search);

        return AdminAnnouncementSearchResult.Successful(page);
    }

    public async Task<UpdateAnnouncementDto?> GetForEditAsync(int announcementId)
    {
        await GetAdministratorUserIdAsync();

        if (announcementId <= 0)
        {
            throw new ValidationException("Annoncen er ugyldig.");
        }

        var announcement = await _announcementRepository.GetByIdForAdministrationAsync(announcementId);

        if (announcement is null)
        {
            return null;
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

            // Administratorbilleder håndteres gennem
            // denne service og ikke ejerens ImageService.
            CanManageImages = true
        };
    }

    public async Task UpdateAsync(UpdateAnnouncementDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var administratorUserId = await GetAdministratorUserIdAsync();

        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, new ValidationContext(request), validationResults, validateAllProperties: true);

        if (!isValid)
        {
            throw new ValidationException(validationResults.First().ErrorMessage ?? "Annonceoplysningerne er ugyldige.");
        }

        if (request.ProductionYear > DateTime.UtcNow.Year + 1)
        {
            throw new ValidationException("Produktionsåret kan ikke ligge i fremtiden.");
        }

        var brandExists = await _carBrandRepository.ExistsAsync(request.BrandId);

        if (!brandExists)
        {
            throw new ValidationException("Det valgte bilmærke findes ikke.");
        }

        var existingAnnouncement = await _announcementRepository.GetByIdForAdministrationAsync(request.AnnouncementId);

        if (existingAnnouncement is null)
        {
            throw new InvalidOperationException("Annoncen findes ikke.");
        }

        var car = new Car
        {
            CarId = existingAnnouncement.CarId,

            BrandId = request.BrandId,

            Model = request.Model.Trim(),

            ProductionYear = checked((short)request.ProductionYear),

            Mileage = request.Mileage,

            Color = NormalizeOptionalText(request.Color)
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

        var wasUpdated = await _announcementRepository.UpdateForAdministrationWithAuditAsync(car, announcement, administratorUserId, Guid.NewGuid(), request.ExpectedUpdatedAt);

        if (!wasUpdated)
        {
            throw new InvalidOperationException("Annoncen kunne ikke opdateres. " + "Den kan være blevet ændret af en anden bruger.");
        }
    }

    public async Task DeactivateAsync(int announcementId)
    {
        var administratorUserId = await GetAdministratorUserIdAsync();

        if (announcementId <= 0)
        {
            throw new ValidationException("Annoncen er ugyldig.");
        }

        var existingAnnouncement = await _announcementRepository.GetByIdForAdministrationAsync(announcementId);

        if (existingAnnouncement is null)
        {
            throw new InvalidOperationException("Annoncen findes ikke.");
        }

        if (!existingAnnouncement.IsActive)
        {
            throw new InvalidOperationException("Annoncen er allerede deaktiveret.");
        }

        var wasDeactivated = await _announcementRepository.DeactivateForAdministrationWithAuditAsync(announcementId, administratorUserId, Guid.NewGuid());

        if (!wasDeactivated)
        {
            throw new InvalidOperationException("Annoncen kunne ikke deaktiveres.");
        }
    }

    public async Task ReactivateAsync(int announcementId)
    {
        // Kontrollér adgang før validering og opslag.
        var administratorUserId = await GetAdministratorUserIdAsync();

        if (announcementId <= 0)
        {
            throw new ValidationException("Annoncen er ugyldig.");
        }

        // Admin-opslaget kan også hente annoncer fra deaktiverede ejere.
        var announcement = await _announcementRepository.GetByIdForAdministrationAsync(announcementId);

        if (announcement is null)
        {
            throw new InvalidOperationException("Annoncen findes ikke.");
        }

        if (announcement.IsActive)
        {
            throw new InvalidOperationException("Annoncen er allerede aktiv.");
        }

        // Repository-laget kontrollerer administratoren igen og
        // gemmer statusændring og auditlog i samme transaktion.
        var wasReactivated = await _announcementRepository.ReactivateForAdministrationWithAuditAsync(announcementId, administratorUserId, Guid.NewGuid());

        if (!wasReactivated)
        {
            throw new InvalidOperationException("Annoncen kunne ikke genaktiveres. Genindlæs siden og prøv igen.");
        }
    }

    public async Task<IReadOnlyList<AnnouncementImageDto>> GetImagesAsync(int announcementId)
    {
        await GetAdministratorUserIdAsync();

        if (announcementId <= 0)
        {
            throw new ValidationException("Annoncen er ugyldig.");
        }

        var announcement = await _announcementRepository.GetByIdForAdministrationAsync(announcementId);

        if (announcement is null)
        {
            return Array.Empty<AnnouncementImageDto>();
        }

        var images = await _imageRepository.GetForAdministrationAsync(announcementId);

        return images.Select(MapImage).ToArray();
    }

    public async Task<bool> DeleteImageAsync(int announcementId, int imageId, CancellationToken cancellationToken = default)
    {
        var administratorUserId = await GetAdministratorUserIdAsync();

        if (announcementId <= 0 || imageId <= 0)
        {
            return false;
        }

        var fileName = await _imageRepository.DeleteForAdministrationWithAuditAsync(announcementId, imageId, administratorUserId, Guid.NewGuid());

        if (fileName is null)
        {
            return false;
        }

        // Databasemetadata og auditlog er nu gemt.
        // Derefter fjernes den fysiske fil.
        await _imageStorage.DeleteAsync(fileName, cancellationToken);

        return true;
    }

    /// <summary>
    /// Returnerer den aktuelle administrators bruger-id
    /// eller afviser handlingen.
    /// </summary>
    private async Task<int> GetAdministratorUserIdAsync()
    {
        var isAuthenticated = await _currentUserService.IsAuthenticatedAsync();

        if (!isAuthenticated)
        {
            throw new UnauthorizedAccessException("Du skal være logget ind.");
        }

        var userId = await _currentUserService.GetUserIdAsync();

        if (userId is null)
        {
            throw new UnauthorizedAccessException("Brugeridentiteten er ugyldig.");
        }

        var isAdministrator = await _currentUserService.IsInRoleAsync("Administrator");

        if (!isAdministrator)
        {
            throw new UnauthorizedAccessException("Du har ikke adgang til annoncemoderation.");
        }

        return userId.Value;
    }

    private AnnouncementImageDto MapImage(AnnouncementImage image)
    {
        return new AnnouncementImageDto
        {
            Id = image.Id,
            AnnouncementId = image.AnnouncementId,
            FileName = image.FileName,
            Url = _imageStorage.GetPublicUrl(image.FileName),
            IsPrimary = image.IsPrimary,
            SortOrder = image.SortOrder
        };
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}