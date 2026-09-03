using BrugtBilsHandelsPortal.Components.Shared;
using BrugtBilsHandelsPortal.DTOs.Images;
using Bunit;
using Xunit;

namespace BrugtBilsHandelsPortal.Tests.Components;

public class ImageGalleryTests
{
    [Fact]
    [Trait("UserStory", "US07")]
    public void ManagedGallery_RequiresConfirmationBeforeDelete()
    {
        using var context = new BunitContext();
        int? deletedImageId = null;
        var cut = context.Render<ImageGallery>(parameters => parameters.Add(component => component.Images, CreateImages()).Add(component => component.CanManage, true).Add(component => component.OnDelete, imageId => deletedImageId = imageId));

        cut.Find("[data-testid='delete-image']").Click();

        Assert.Null(deletedImageId);
        var confirmation = cut.Find("[data-testid='delete-image-confirmation']");
        Assert.Contains("Er du sikker", confirmation.TextContent);

        cut.Find("[data-testid='delete-image-confirmation'] button.btn-danger").Click();

        Assert.Equal(1, deletedImageId);
    }

    [Fact]
    [Trait("UserStory", "US07")]
    public void ReadOnlyGallery_DoesNotShowImageActions()
    {
        using var context = new BunitContext();
        var cut = context.Render<ImageGallery>(parameters => parameters.Add(component => component.Images, CreateImages()));

        Assert.Empty(cut.FindAll("[data-testid='delete-image']"));
        Assert.DoesNotContain("Sæt som primært billede", cut.Markup);
    }

    private static IReadOnlyList<AnnouncementImageDto> CreateImages()
    {
        return[new AnnouncementImageDto
            {
                Id = 1,
                AnnouncementId = 42,
                FileName = "octavia.jpg",
                Url = "/uploads/octavia.jpg",
                IsPrimary = true,
                SortOrder = 0
            }
        ];
    }
}