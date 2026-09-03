using BrugtBilsHandelsPortal.DTOs.Images;
using BrugtBilsHandelsPortal.Models.Entities;

namespace BrugtBilsHandelsPortal.Data.Repositories.Interfaces;

/// <summary>Definerer ejerbegrænsede ændringer af billedmetadata.</summary>
public interface IImageRepository
{
    Task<ImageMetadataCreateResult> AddForOwnerAsync(int announcementId, int userId, string fileName, int maxImages);

    Task<IReadOnlyList<AnnouncementImage>> GetForActiveAnnouncementAsync(int announcementId);

    Task<bool> SetPrimaryForOwnerAsync(int announcementId, int imageId, int userId);

    Task<string?> DeleteForOwnerAsync(int announcementId, int imageId, int userId);

    /// <summary>
    /// Henter alle billeder til en annonce uanset annoncens
    /// aktiv-status. Bruges kun efter admin-autorisation.
    /// </summary>
    Task<IReadOnlyList<AnnouncementImage>> GetForAdministrationAsync(int announcementId);

    /// <summary>
    /// Fjerner billedmetadata som administrator og opretter
    /// en auditlog i samme databasetransaktion.
    ///
    /// Returnerer filnavnet, så service-laget efterfølgende
    /// kan fjerne den fysiske fil.
    /// </summary>
    Task<string?> DeleteForAdministrationWithAuditAsync(int announcementId, int imageId, int administratorUserId, Guid correlationId);
}