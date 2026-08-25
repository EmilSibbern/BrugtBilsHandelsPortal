using BrugtBilsHandelsPortal.Components.Pages.Announcements;
using BrugtBilsHandelsPortal.DTOs.Announcements;
using BrugtBilsHandelsPortal.DTOs.Comments;
using BrugtBilsHandelsPortal.DTOs.Images;
using BrugtBilsHandelsPortal.Models.Entities;
using BrugtBilsHandelsPortal.Security;
using BrugtBilsHandelsPortal.Services.Interfaces;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using System.Text.RegularExpressions;
using Xunit;

namespace BrugtBilsHandelsPortal.Tests.Components;

public class AnnouncementDetailsTests
{
    [Fact]
    [Trait("UserStory", "US05")]
    public void Guest_CanSeeAllPublicAnnouncementDetails()
    {
        using var context = CreateContext(
            announcement: CreateAnnouncement(),
            images: CreateImages());

        var cut = RenderDetails(context);

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Skoda Octavia", cut.Markup);
            Assert.Contains("Velholdt familiebil", cut.Markup);
            var renderedDigits = Regex.Replace(cut.Markup, "[^0-9]", "");
            Assert.Contains("175000", renderedDigits);
            Assert.Contains("Skoda", cut.Markup);
            Assert.Contains("Octavia", cut.Markup);
            Assert.Contains("2020", cut.Markup);
            Assert.Contains("85000", renderedDigits);
            Assert.Contains("kr.", cut.Markup);
            Assert.Contains("km", cut.Markup);
            Assert.Contains("Sort", cut.Markup);
            Assert.Contains("sælger", cut.Markup);

            var image = cut.Find("img[src='/uploads/octavia.jpg']");
            Assert.Equal("Billede af annoncen", image.GetAttribute("alt"));
        });
    }

    [Fact]
    [Trait("UserStory", "US05")]
    public void UnknownOrInactiveAnnouncement_ShowsNotFoundState()
    {
        using var context = CreateContext(announcement: null);

        var cut = RenderDetails(context);

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(
                "Annoncen findes ikke eller er ikke længere aktiv",
                cut.Markup);
            Assert.NotNull(cut.Find("a[href='/']"));
        });
    }

    [Fact]
    [Trait("UserStory", "US05")]
    public void BackendFailure_ShowsSafeErrorWithoutTechnicalDetails()
    {
        using var context = CreateContext(
            detailsException: new InvalidOperationException(
                "SELECT PasswordHash FROM Users"));

        var cut = RenderDetails(context);

        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find("[role='alert']");
            Assert.Contains(
                "Annoncen kunne ikke indlæses. Prøv igen.",
                alert.TextContent);
            Assert.DoesNotContain("PasswordHash", cut.Markup);
            Assert.DoesNotContain("SELECT", cut.Markup);
        });
    }

    [Fact]
    [Trait("UserStory", "US05")]
    public void Guest_CanSeePublicComments()
    {
        using var context = CreateContext(
            announcement: CreateAnnouncement());

        var cut = RenderDetails(context);

        cut.WaitForAssertion(() =>
        {
            var comments = cut.Find("[data-testid='public-comments']");
            Assert.Contains("Er bilen stadig til salg?", comments.TextContent);
        });
    }

    [Fact]
    [Trait("UserStory", "US05")]
    public void AuthorizedUser_CanStartContactAndCommentFlows()
    {
        using var context = CreateContext(
            announcement: CreateAnnouncement(),
            currentUserId: 99,
            isAuthorized: true,
            roles: ["User"]);

        var cut = RenderDetails(context);

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.Find("[data-testid='contact-seller']"));
            Assert.NotNull(cut.Find("[data-testid='comment-form']"));
            Assert.Empty(cut.FindAll("[data-testid='login-register-modal']"));
        });
    }

    [Theory]
    [InlineData("contact-seller")]
    [InlineData("add-comment")]
    [Trait("UserStory", "US05")]
    public void Guest_ProtectedActionOpensLoginRegisterModal(
        string actionTestId)
    {
        using var context = CreateContext(
            announcement: CreateAnnouncement());

        var cut = RenderDetails(context);

        cut.Find($"[data-testid='{actionTestId}']").Click();

        cut.WaitForAssertion(() =>
            Assert.NotNull(
                cut.Find("[data-testid='login-register-modal']")));
    }

    [Fact]
    [Trait("UserStory", "US05")]
    public void Administrator_SeesModerationControls()
    {
        using var context = CreateContext(
            announcement: CreateAnnouncement(),
            currentUserId: 1,
            isAuthorized: true,
            roles: ["Administrator"]);

        var cut = RenderDetails(context);

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(
                cut.Find("[data-testid='admin-announcement-actions']"));
            Assert.NotNull(
                cut.Find("[data-testid='admin-comment-actions']"));
        });
    }

    private static BunitContext CreateContext(
        AnnouncementDetailsDto? announcement = null,
        IReadOnlyList<AnnouncementImageDto>? images = null,
        int? currentUserId = null,
        bool isAuthorized = false,
        string[]? roles = null,
        Exception? detailsException = null)
    {
        var context = new BunitContext();
        var authorization = context.AddAuthorization();

        if (isAuthorized)
        {
            authorization.SetAuthorized("test-user");
            authorization.SetRoles(roles ?? ["User"]);
        }

        context.Services.AddSingleton<IAnnouncementService>(
            new DetailsAnnouncementService(
                announcement,
                detailsException));
        context.Services.AddSingleton<IImageService>(
            new DetailsImageService(images ?? []));
        context.Services.AddSingleton<ICurrentUserService>(
            new DetailsCurrentUserService(currentUserId));
        context.Services.AddSingleton<ICommentService>(
            new DetailsCommentService(
            [
                new CommentDto
                {
                    Id = 8,
                    AnnouncementId = 42,
                    UserId = 99,
                    Username = "køber",
                    Text = "Er bilen stadig til salg?",
                    CreatedAt = new DateTime(2026, 8, 23)
                }
            ]));

        return context;
    }

    private static IRenderedComponent<AnnouncementDetails> RenderDetails(
        BunitContext context)
    {
        return context.Render<AnnouncementDetails>(
            parameters => parameters.Add(page => page.Id, 42));
    }

    private static AnnouncementDetailsDto CreateAnnouncement()
    {
        return new AnnouncementDetailsDto
        {
            Id = 42,
            Title = "Skoda Octavia",
            Description = "Velholdt familiebil.",
            Price = 175_000,
            CreatedAt = new DateTime(2026, 8, 23),
            SellerUserId = 7,
            SellerUsername = "sælger",
            CarId = 10,
            BrandId = 1,
            BrandName = "Skoda",
            Model = "Octavia",
            ProductionYear = 2020,
            Mileage = 85_000,
            Color = "Sort"
        };
    }

    private static IReadOnlyList<AnnouncementImageDto> CreateImages()
    {
        return
        [
            new AnnouncementImageDto
            {
                Id = 5,
                AnnouncementId = 42,
                FileName = "octavia.jpg",
                Url = "/uploads/octavia.jpg",
                IsPrimary = true,
                SortOrder = 0
            }
        ];
    }

    private sealed class DetailsAnnouncementService(
        AnnouncementDetailsDto? announcement,
        Exception? detailsException) : IAnnouncementService
    {
        public Task<AnnouncementDetailsDto?> GetDetailsAsync(
            int announcementId)
        {
            if (detailsException is not null)
            {
                return Task.FromException<AnnouncementDetailsDto?>(
                    detailsException);
            }

            return Task.FromResult(announcement);
        }

        public Task<IReadOnlyList<CarBrand>> GetCarBrandsAsync() =>
            throw new NotSupportedException();

        public Task<CreateAnnouncementResult> CreateAsync(
            CreateAnnouncementDto request) =>
            throw new NotSupportedException();

        public Task<AnnouncementSearchResult> SearchActiveAsync(
            AnnouncementSearchDto search) =>
            throw new NotSupportedException();
    }

    private sealed class DetailsImageService(
        IReadOnlyList<AnnouncementImageDto> images) : IImageService
    {
        public Task<IReadOnlyList<AnnouncementImageDto>>
            GetForActiveAnnouncementAsync(int announcementId) =>
            Task.FromResult(images);

        public Task<ImageUploadResult> UploadAsync(
            UploadAnnouncementImageRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> SetPrimaryAsync(
            int announcementId,
            int imageId) =>
            throw new NotSupportedException();
    }

    private sealed class DetailsCurrentUserService(int? userId)
        : ICurrentUserService
    {
        public Task<int?> GetUserIdAsync() =>
            Task.FromResult(userId);

        public Task<bool> IsAuthenticatedAsync() =>
            Task.FromResult(userId.HasValue);

        public Task<bool> IsInRoleAsync(string role) =>
            Task.FromResult(false);
    }

    private sealed class DetailsCommentService(
        IReadOnlyList<CommentDto> comments) : ICommentService
    {
        public Task<IReadOnlyList<CommentDto>>
            GetForAnnouncementAsync(int announcementId) =>
            Task.FromResult(comments);

        public Task<CommentDto> CreateAsync(
            CreateCommentDto request) =>
            throw new NotSupportedException();

        public Task DeleteAsync(long commentId) =>
            throw new NotSupportedException();
    }
}
