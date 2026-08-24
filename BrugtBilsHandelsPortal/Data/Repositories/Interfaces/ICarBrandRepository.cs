using BrugtBilsHandelsPortal.Models.Entities;

namespace BrugtBilsHandelsPortal.Data.Repositories.Interfaces;

/// <summary>Definerer læseoperationer for kataloget over bilmærker.</summary>
public interface ICarBrandRepository
{
    Task<IReadOnlyList<CarBrand>> GetAllAsync();
    Task<bool> ExistsAsync(int brandId);
}
