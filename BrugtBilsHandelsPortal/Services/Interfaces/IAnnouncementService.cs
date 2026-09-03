using BrugtBilsHandelsPortal.DTOs.Announcements;
using BrugtBilsHandelsPortal.Models.Entities;

namespace BrugtBilsHandelsPortal.Services.Interfaces;

/// <summary>
/// Orkestrerer annoncefunktioner og håndhæver validering og adgangsregler før datalaget kaldes.
/// </summary>
public interface IAnnouncementService
{
    /// <summary>Henter de bilmærker, som kan vælges ved oprettelse og filtrering.</summary>
    Task<IReadOnlyList<CarBrand>> GetCarBrandsAsync();

    /// <summary>Validerer og opretter en annonce for den aktuelle bruger.</summary>
    Task<CreateAnnouncementResult> CreateAsync(CreateAnnouncementDto request);

    /// <summary>Henter annoncefelterne til redigering for ejer eller administrator.</summary>
    Task<UpdateAnnouncementDto?> GetForEditAsync(int announcementId);

    /// <summary>Validerer og opdaterer en annonce for ejer eller administrator.</summary>
    Task UpdateAsync(UpdateAnnouncementDto request);

    /// <summary>Henter den aktuelle brugers egne annoncer.</summary>
    Task<IReadOnlyList<AnnouncementListItemDto>> GetMineAsync();

    /// <summary>
    /// Aktiverer en inaktiv annonce, hvis den aktuelle bruger ejer den.
    /// </summary>
    Task ActivateAsync(int announcementId);

    /// <summary>
    /// Deaktiverer en aktiv annonce, hvis den aktuelle bruger ejer den.
    /// </summary>
    Task DeactivateAsync(int announcementId);

    /// <summary>
    /// Henter en annonce, hvis den er aktiv, eller hvis den aktuelle bruger må se en inaktiv annonce.
    /// </summary>
    Task<AnnouncementDetailsDto?> GetDetailsAsync(int announcementId);

    /// <summary>Validerer søgekriterierne og returnerer én side med aktive annoncer.</summary>
    Task<AnnouncementSearchResult> SearchActiveAsync(AnnouncementSearchDto search);
}