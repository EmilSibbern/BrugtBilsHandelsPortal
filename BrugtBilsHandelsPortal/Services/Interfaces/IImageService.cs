using BrugtBilsHandelsPortal.DTOs.Images;

namespace BrugtBilsHandelsPortal.Services.Interfaces;

/// <summary>Validerer billedfiler og koordinerer fysisk lagring med metadata i databasen.</summary>
public interface IImageService
{
    /// <summary>Validerer og gemmer et billede på en annonce, som brugeren ejer.</summary>
    Task<ImageUploadResult> UploadAsync(UploadAnnouncementImageRequest request, CancellationToken cancellationToken = default);

    /// <summary>Henter offentlige billedoplysninger til en aktiv annonce.</summary>
    Task<IReadOnlyList<AnnouncementImageDto>> GetForActiveAnnouncementAsync(int announcementId);

    /// <summary>Vælger primærbilledet, hvis den aktuelle bruger ejer annoncen.</summary>
    Task<bool> SetPrimaryAsync(int announcementId, int imageId);

    /// <summary>
    /// Fjerner et billede, hvis den aktuelle bruger ejer annoncen.
    /// Hvis primærbilledet fjernes, vælger repository-laget et nyt.
    /// </summary>
    Task<bool> DeleteAsync(int announcementId, int imageId, CancellationToken cancellationToken = default);
}