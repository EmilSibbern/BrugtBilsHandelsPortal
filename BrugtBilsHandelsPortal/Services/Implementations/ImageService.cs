using BrugtBilsHandelsPortal.Configuration;
using BrugtBilsHandelsPortal.Data.Repositories.Interfaces;
using BrugtBilsHandelsPortal.DTOs.Images;
using BrugtBilsHandelsPortal.Security;
using BrugtBilsHandelsPortal.Services.Interfaces;
using BrugtBilsHandelsPortal.Storage;
using BrugtBilsHandelsPortal.Validation;
using Microsoft.Extensions.Options;

namespace BrugtBilsHandelsPortal.Services.Implementations;

/// <summary>Koordinerer sikker filvalidering, disklagring og billedmetadata.</summary>
public class ImageService : IImageService
{
    private readonly IImageRepository _imageRepository;
    private readonly IImageStorage _imageStorage;
    private readonly ICurrentUserService _currentUserService;
    private readonly ImageUploadOptions _options;

    public ImageService(
        IImageRepository imageRepository,
        IImageStorage imageStorage,
        ICurrentUserService currentUserService,
        IOptions<ImageUploadOptions> options)
    {
        _imageRepository = imageRepository;
        _imageStorage = imageStorage;
        _currentUserService = currentUserService;
        _options = options.Value;
    }

    public async Task<ImageUploadResult> UploadAsync(
        UploadAnnouncementImageRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.AnnouncementId <= 0)
        {
            return ImageUploadResult.Failed(
                "Annoncen er ugyldig.");
        }

        var userId =
            await _currentUserService.GetUserIdAsync();

        if (userId is null)
        {
            return ImageUploadResult.Failed(
                "Du skal være logget ind for at uploade billeder.");
        }

        if (request.Size <= 0)
        {
            return ImageUploadResult.Failed(
                "Billedfilen er tom.");
        }

        if (request.Size > _options.MaxFileSizeBytes)
        {
            return ImageUploadResult.Failed(
                $"Billedet må højst fylde " +
                $"{_options.MaxFileSizeBytes / 1024 / 1024} MB.");
        }

        await using var validatedContent =
            await ReadValidatedContentAsync(
                request.Content,
                _options.MaxFileSizeBytes,
                cancellationToken);

        if (validatedContent is null)
        {
            return ImageUploadResult.Failed(
                "Billedet overskrider den tilladte størrelse.");
        }

        if (!ImageValidation.TryGetExtension(
                validatedContent.GetBuffer()
                    .AsSpan(
                        0,
                        checked((int)validatedContent.Length)),
                request.ContentType,
                out var extension))
        {
            return ImageUploadResult.Failed(
                "Kun gyldige JPEG-, PNG- og WebP-billeder er tilladt.");
        }

        validatedContent.Position = 0;

        string? fileName = null;

        try
        {
            fileName = await _imageStorage.SaveAsync(
                validatedContent,
                extension,
                cancellationToken);

            var metadataResult =
                await _imageRepository.AddForOwnerAsync(
                    request.AnnouncementId,
                    userId.Value,
                    fileName,
                    _options.MaxImagesPerAnnouncement);

            if (metadataResult.Status !=
                ImageMetadataCreateStatus.Success
                || metadataResult.Image is null)
            {
                await _imageStorage.DeleteAsync(
                    fileName,
                    cancellationToken);

                return metadataResult.Status switch
                {
                    ImageMetadataCreateStatus
                        .ImageLimitReached =>
                        ImageUploadResult.Failed(
                            $"En annonce kan højst have " +
                            $"{_options.MaxImagesPerAnnouncement} billeder."),

                    _ =>
                        ImageUploadResult.Failed(
                            "Annoncen findes ikke, eller du ejer den ikke.")
                };
            }

            return ImageUploadResult.Successful(
                MapImage(metadataResult.Image));
        }
        catch
        {
            if (fileName is not null)
            {
                await _imageStorage.DeleteAsync(
                    fileName,
                    cancellationToken);
            }

            throw;
        }
    }

    public async Task<IReadOnlyList<AnnouncementImageDto>>
        GetForActiveAnnouncementAsync(
            int announcementId)
    {
        if (announcementId <= 0)
        {
            return Array.Empty<AnnouncementImageDto>();
        }

        var images =
            await _imageRepository
                .GetForActiveAnnouncementAsync(
                    announcementId);

        return images
            .Select(MapImage)
            .ToArray();
    }

    public async Task<bool> SetPrimaryAsync(
        int announcementId,
        int imageId)
    {
        if (announcementId <= 0 || imageId <= 0)
        {
            return false;
        }

        var userId =
            await _currentUserService.GetUserIdAsync();

        if (userId is null)
        {
            return false;
        }

        return await _imageRepository.SetPrimaryForOwnerAsync(
            announcementId,
            imageId,
            userId.Value);
    }

    private AnnouncementImageDto MapImage(
        Models.Entities.AnnouncementImage image)
    {
        return new AnnouncementImageDto
        {
            Id = image.Id,
            AnnouncementId = image.AnnouncementId,
            FileName = image.FileName,
            Url = _imageStorage.GetPublicUrl(
                image.FileName),
            IsPrimary = image.IsPrimary,
            SortOrder = image.SortOrder
        };
    }

    private static async Task<MemoryStream?>
        ReadValidatedContentAsync(
            Stream source,
            long maxFileSize,
            CancellationToken cancellationToken)
    {
        var content = new MemoryStream();
        var buffer = new byte[81920];
        long totalBytes = 0;

        while (true)
        {
            var bytesRead = await source.ReadAsync(
                buffer,
                cancellationToken);

            if (bytesRead == 0)
            {
                break;
            }

            totalBytes += bytesRead;

            if (totalBytes > maxFileSize)
            {
                await content.DisposeAsync();
                return null;
            }

            await content.WriteAsync(
                buffer.AsMemory(0, bytesRead),
                cancellationToken);
        }

        content.Position = 0;

        return content;
    }
}
