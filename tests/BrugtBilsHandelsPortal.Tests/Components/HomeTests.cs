using BrugtBilsHandelsPortal.Components.Pages;
using BrugtBilsHandelsPortal.DTOs.Announcements;
using BrugtBilsHandelsPortal.DTOs.Common;
using BrugtBilsHandelsPortal.Models.Entities;
using BrugtBilsHandelsPortal.Services.Interfaces;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BrugtBilsHandelsPortal.Tests.Components;

public class HomeTests
{
    [Fact]
    [Trait("UserStory", "US10")]
    public void SearchForm_SendsFiltersToService()
    {
        using var context = CreateContext();

        var service = new HomeAnnouncementService();
        context.Services.AddSingleton<IAnnouncementService>(service);

        var cut = context.Render<Home>();

        cut.WaitForAssertion(() => Assert.Contains("Skoda Octavia", cut.Markup));

        cut.Find("#query").Change("Octavia");
        cut.Find("#brand").Change("1");
        cut.Find("#min-price").Change("100000");
        cut.Find("#max-price").Change("200000");
        cut.Find("#year").Change("2020");
        cut.Find("#mileage").Change("90000");
        cut.Find("#sort").Change("price-asc");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(2, service.SearchCallCount);
            Assert.NotNull(service.LastSearch);
            Assert.Equal("Octavia", service.LastSearch.Query);
            Assert.Equal(1, service.LastSearch.BrandId);
            Assert.Equal(100_000, service.LastSearch.MinPrice);
            Assert.Equal(200_000, service.LastSearch.MaxPrice);
            Assert.Equal(2020, service.LastSearch.MinProductionYear);
            Assert.Equal(90_000, service.LastSearch.MaxMileage);
            Assert.Equal("price-asc", service.LastSearch.Sort);
            Assert.Equal(1, service.LastSearch.Page);
        });

        Assert.Contains("Søgning: Octavia", cut.Markup);
        Assert.Contains("Mærke: Skoda", cut.Markup);
    }

    [Fact]
    [Trait("UserStory", "US10")]
    public void EmptyFilteredSearch_ShowsNoResultsState()
    {
        using var context = CreateContext();

        var service = new HomeAnnouncementService
        {
            ReturnEmptyResult = true
        };

        context.Services.AddSingleton<IAnnouncementService>(service);

        var cut = context.Render<Home>();

        cut.WaitForAssertion(() => Assert.Contains("Ingen aktive annoncer", cut.Markup));

        cut.Find("#query").Change("Findes ikke");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Ingen resultater", cut.Markup);
            Assert.Contains("Prøv at ændre søgningen eller nulstil filtrene.", cut.Markup);
        });
    }

    private static BunitContext CreateContext()
    {
        var context = new BunitContext();
        context.AddAuthorization();

        return context;
    }

    private sealed class HomeAnnouncementService : IAnnouncementService
    {
        public int SearchCallCount { get; private set; }
        public AnnouncementSearchDto? LastSearch { get; private set; }
        public bool ReturnEmptyResult { get; set; }

        public Task<IReadOnlyList<CarBrand>> GetCarBrandsAsync()
        {
            IReadOnlyList<CarBrand> brands = [new CarBrand
                {
                    BrandId = 1,
                    BrandName = "Skoda"
                }
            ];

            return Task.FromResult(brands);
        }

        public Task<AnnouncementSearchResult> SearchActiveAsync(AnnouncementSearchDto search)
        {
            SearchCallCount++;
            LastSearch = search;

            var items = ReturnEmptyResult ? Array.Empty<AnnouncementListItemDto>() : [new AnnouncementListItemDto
                    {
                        Id = 42,
                        Title = "Skoda Octavia",
                        Price = 175_000,
                        CreatedAt = new DateTime(2026, 8, 23),
                        BrandName = "Skoda",
                        Model = "Octavia",
                        ProductionYear = 2020,
                        Mileage = 85_000
                    }
                ];

            var page = new PagedResult<AnnouncementListItemDto>
                {
                    Items = items,
                    Page = search.Page,
                    PageSize = search.PageSize,
                    TotalItems = items.Length
                };

            return Task.FromResult(AnnouncementSearchResult.Successful(page));
        }

        public Task<CreateAnnouncementResult> CreateAsync(CreateAnnouncementDto request) => throw new NotSupportedException();

        public Task<UpdateAnnouncementDto?> GetForEditAsync(int announcementId) => throw new NotSupportedException();

        public Task UpdateAsync(UpdateAnnouncementDto request) => throw new NotSupportedException();

        public Task<IReadOnlyList<AnnouncementListItemDto>> GetMineAsync() => throw new NotSupportedException();

        public Task ActivateAsync(int announcementId) => throw new NotSupportedException();

        public Task DeactivateAsync(int announcementId) => throw new NotSupportedException();

        public Task<AnnouncementDetailsDto?> GetDetailsAsync(int announcementId) => throw new NotSupportedException();
    }
}