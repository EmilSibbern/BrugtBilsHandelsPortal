using BrugtBilsHandelsPortal.Data.Repositories.Interfaces;
using BrugtBilsHandelsPortal.Models.Entities;

namespace BrugtBilsHandelsPortal.Tests.Fakes;

public class FakeCarBrandRepository : ICarBrandRepository
{
    public List<CarBrand> Brands { get; } = [];

    public int ExistsCallCount { get; private set; }

    public Task<IReadOnlyList<CarBrand>> GetAllAsync()
    {
        return Task.FromResult<IReadOnlyList<CarBrand>>(Brands);
    }

    public Task<bool> ExistsAsync(int brandId)
    {
        ExistsCallCount++;

        return Task.FromResult(
            Brands.Any(brand => brand.BrandId == brandId));
    }
}
