using BrugtBilsHandelsPortal.Models.Entities;

namespace BrugtBilsHandelsPortal.Data.Repositories.Interfaces;

public interface ICarBrandRepository
{
    Task<IReadOnlyList<CarBrand>> GetAllAsync();
    Task<bool> ExistsAsync(int brandId);
}