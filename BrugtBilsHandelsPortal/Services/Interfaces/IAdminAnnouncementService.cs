using BrugtBilsHandelsPortal.DTOs.Announcements;
using BrugtBilsHandelsPortal.DTOs.Images;

namespace BrugtBilsHandelsPortal.Services.Interfaces;

/// <summary>
/// Administrationsfunktioner til søgning og moderation
/// af alle brugeres annoncer.
/// </summary>
public interface IAdminAnnouncementService
{
    /// <summary>
    /// Søger blandt alle annoncer med mulighed for at
    /// filtrere på status og ejer.
    /// </summary>
    Task<AdminAnnouncementSearchResult> SearchAsync(AdminAnnouncementSearchDto search);

    /// <summary>
    /// Henter en annonce til administrativ redigering.
    /// </summary>
    Task<UpdateAnnouncementDto?> GetForEditAsync(int announcementId);

    /// <summary>
    /// Validerer og opdaterer en annonce som administrator.
    /// Handlingen skal registreres i auditloggen.
    /// </summary>
    Task UpdateAsync(UpdateAnnouncementDto request);

    /// <summary>
    /// Deaktiverer en annonce som administrator.
    /// Handlingen skal registreres i auditloggen.
    /// </summary>
    Task DeactivateAsync(int announcementId);

    /// <summary>
    /// Fjerner et upassende billede fra en annonce.
    /// Handlingen skal registreres i auditloggen.
    /// </summary>
    Task<bool> DeleteImageAsync(int announcementId, int imageId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AnnouncementImageDto>> GetImagesAsync(int announcementId);

    /// <summary>
    /// Genaktiverer en inaktiv annonce som administrator.
    /// Handlingen registreres i auditloggen.
    /// </summary>
    Task ReactivateAsync(int announcementId);
}