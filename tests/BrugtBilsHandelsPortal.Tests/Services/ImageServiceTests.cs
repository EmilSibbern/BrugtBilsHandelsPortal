using BrugtBilsHandelsPortal.Configuration;
using BrugtBilsHandelsPortal.DTOs.Images;
using BrugtBilsHandelsPortal.Models.Entities;
using BrugtBilsHandelsPortal.Services.Implementations;
using BrugtBilsHandelsPortal.Tests.Fakes;
using Microsoft.Extensions.Options;
using Xunit;

namespace BrugtBilsHandelsPortal.Tests.Services;

public class ImageServiceTests
{
    [Fact]
    public async Task UploadAsync_WithValidJpeg_SavesFileAndMetadataForCurrentUser()
    {
        var repository = new FakeImageRepository();
        var storage = new FakeImageStorage();
        var content = CreateJpeg();
        var service = CreateService(
            repository,
            storage,
            userId: 7);

        var result = await service.UploadAsync(
            CreateRequest(content, "image/jpeg"));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Image);
        Assert.Equal(42, result.Image.AnnouncementId);
        Assert.True(result.Image.IsPrimary);
        Assert.Equal("/uploads/generated-image.jpg", result.Image.Url);

        Assert.Equal(1, storage.SaveCallCount);
        Assert.Equal("jpg", storage.LastExtension);
        Assert.Equal(content, storage.LastSavedContent);
        Assert.Equal(0, storage.DeleteCallCount);

        Assert.Equal(1, repository.AddCallCount);
        Assert.Equal(42, repository.LastAnnouncementId);
        Assert.Equal(7, repository.LastUserId);
        Assert.Equal("generated-image.jpg", repository.LastFileName);
        Assert.Equal(10, repository.LastMaxImages);
    }

    [Fact]
    public async Task UploadAsync_WithoutAuthentication_IsRejectedBeforeStorageAndRepository()
    {
        var repository = new FakeImageRepository();
        var storage = new FakeImageStorage();
        var service = CreateService(
            repository,
            storage,
            userId: null);

        var result = await service.UploadAsync(
            CreateRequest(CreateJpeg(), "image/jpeg"));

        Assert.False(result.Succeeded);
        Assert.Contains("logget ind", result.ErrorMessage);
        Assert.Equal(0, storage.SaveCallCount);
        Assert.Equal(0, repository.AddCallCount);
    }

    [Fact]
    public async Task UploadAsync_WithInvalidSignature_IsRejected()
    {
        var repository = new FakeImageRepository();
        var storage = new FakeImageStorage();
        var service = CreateService(
            repository,
            storage,
            userId: 7);

        var result = await service.UploadAsync(
            CreateRequest(
                "not an image"u8.ToArray(),
                "image/jpeg"));

        Assert.False(result.Succeeded);
        Assert.Contains("JPEG", result.ErrorMessage);
        Assert.Equal(0, storage.SaveCallCount);
        Assert.Equal(0, repository.AddCallCount);
    }

    [Fact]
    public async Task UploadAsync_WithDeclaredOversize_IsRejectedBeforeReadingStream()
    {
        var repository = new FakeImageRepository();
        var storage = new FakeImageStorage();
        var service = CreateService(
            repository,
            storage,
            userId: 7,
            maxFileSize: 12);

        var request = CreateRequest(
            CreateJpeg(),
            "image/jpeg");

        request = new UploadAnnouncementImageRequest
        {
            AnnouncementId = request.AnnouncementId,
            Content = request.Content,
            ContentType = request.ContentType,
            Size = 13
        };

        var result = await service.UploadAsync(request);

        Assert.False(result.Succeeded);
        Assert.Contains("højst", result.ErrorMessage);
        Assert.Equal(0, storage.SaveCallCount);
        Assert.Equal(0, repository.AddCallCount);
    }

    [Fact]
    public async Task UploadAsync_WhenActualContentExceedsLimit_IsRejected()
    {
        var repository = new FakeImageRepository();
        var storage = new FakeImageStorage();
        var service = CreateService(
            repository,
            storage,
            userId: 7,
            maxFileSize: 4);

        var content = CreateJpeg();

        var result = await service.UploadAsync(
            new UploadAnnouncementImageRequest
            {
                AnnouncementId = 42,
                Content = new MemoryStream(content),
                ContentType = "image/jpeg",
                Size = 4
            });

        Assert.False(result.Succeeded);
        Assert.Contains("overskrider", result.ErrorMessage);
        Assert.Equal(0, storage.SaveCallCount);
        Assert.Equal(0, repository.AddCallCount);
    }

    [Fact]
    public async Task UploadAsync_WhenUserDoesNotOwnAnnouncement_DeletesSavedFile()
    {
        var repository = new FakeImageRepository
        {
            AddStatus = ImageMetadataCreateStatus
                .AnnouncementNotFoundOrNotOwned
        };

        var storage = new FakeImageStorage();
        var service = CreateService(
            repository,
            storage,
            userId: 7);

        var result = await service.UploadAsync(
            CreateRequest(CreateJpeg(), "image/jpeg"));

        Assert.False(result.Succeeded);
        Assert.Contains("ejer", result.ErrorMessage);
        Assert.Equal(1, storage.SaveCallCount);
        Assert.Equal(1, storage.DeleteCallCount);
        Assert.Equal(
            "generated-image.jpg",
            storage.LastDeletedFileName);
    }

    [Fact]
    public async Task UploadAsync_WhenImageLimitIsReached_DeletesSavedFile()
    {
        var repository = new FakeImageRepository
        {
            AddStatus = ImageMetadataCreateStatus
                .ImageLimitReached
        };

        var storage = new FakeImageStorage();
        var service = CreateService(
            repository,
            storage,
            userId: 7);

        var result = await service.UploadAsync(
            CreateRequest(CreateJpeg(), "image/jpeg"));

        Assert.False(result.Succeeded);
        Assert.Contains("højst have 10", result.ErrorMessage);
        Assert.Equal(1, storage.SaveCallCount);
        Assert.Equal(1, storage.DeleteCallCount);
    }

    [Fact]
    public async Task GetForActiveAnnouncementAsync_ReturnsImagesWithPublicUrls()
    {
        var repository = new FakeImageRepository();

        repository.Images.Add(
            new AnnouncementImage
            {
                Id = 1,
                AnnouncementId = 42,
                FileName = "primary.jpg",
                IsPrimary = true,
                SortOrder = 0
            });

        var service = CreateService(
            repository,
            new FakeImageStorage(),
            userId: null);

        var result =
            await service.GetForActiveAnnouncementAsync(42);

        var image = Assert.Single(result);
        Assert.Equal(1, image.Id);
        Assert.Equal("/uploads/primary.jpg", image.Url);
        Assert.True(image.IsPrimary);
        Assert.Equal(1, repository.GetCallCount);
    }

    [Fact]
    public async Task SetPrimaryAsync_WithOwner_SetsSelectedImage()
    {
        var repository = new FakeImageRepository();
        var service = CreateService(
            repository,
            new FakeImageStorage(),
            userId: 7);

        var result = await service.SetPrimaryAsync(42, 9);

        Assert.True(result);
        Assert.Equal(1, repository.SetPrimaryCallCount);
        Assert.Equal(42, repository.LastAnnouncementId);
        Assert.Equal(9, repository.LastPrimaryImageId);
        Assert.Equal(7, repository.LastUserId);
    }

    [Fact]
    public async Task SetPrimaryAsync_WithoutAuthentication_IsRejected()
    {
        var repository = new FakeImageRepository();
        var service = CreateService(
            repository,
            new FakeImageStorage(),
            userId: null);

        var result = await service.SetPrimaryAsync(42, 9);

        Assert.False(result);
        Assert.Equal(0, repository.SetPrimaryCallCount);
    }

    [Theory]
    [InlineData(0, 9)]
    [InlineData(42, 0)]
    public async Task SetPrimaryAsync_WithInvalidId_IsRejected(
        int announcementId,
        int imageId)
    {
        var repository = new FakeImageRepository();
        var service = CreateService(
            repository,
            new FakeImageStorage(),
            userId: 7);

        var result = await service.SetPrimaryAsync(
            announcementId,
            imageId);

        Assert.False(result);
        Assert.Equal(0, repository.SetPrimaryCallCount);
    }

    [Fact]
    [Trait("UserStory", "US07")]
    public async Task DeleteAsync_AsOwner_RemovesMetadataAndPhysicalFile()
    {
        var repository = new FakeImageRepository
        {
            DeleteFileNameToReturn = "octavia.jpg"
        };
        var storage = new FakeImageStorage();
        var service = CreateService(
            repository,
            storage,
            userId: 7);

        var result = await service.DeleteAsync(42, 9);

        Assert.True(result);
        Assert.Equal(1, repository.DeleteCallCount);
        Assert.Equal(42, repository.LastAnnouncementId);
        Assert.Equal(9, repository.LastDeletedImageId);
        Assert.Equal(7, repository.LastUserId);
        Assert.Equal(1, storage.DeleteCallCount);
        Assert.Equal("octavia.jpg", storage.LastDeletedFileName);
    }

    [Fact]
    [Trait("UserStory", "US07")]
    public async Task DeleteAsync_WithoutAuthentication_IsRejectedBeforeRepositoryCall()
    {
        var repository = new FakeImageRepository();
        var storage = new FakeImageStorage();
        var service = CreateService(
            repository,
            storage,
            userId: null);

        var result = await service.DeleteAsync(42, 9);

        Assert.False(result);
        Assert.Equal(0, repository.DeleteCallCount);
        Assert.Equal(0, storage.DeleteCallCount);
    }

    [Fact]
    [Trait("UserStory", "US07")]
    public async Task DeleteAsync_WhenOwnershipCheckFails_DoesNotDeletePhysicalFile()
    {
        var repository = new FakeImageRepository
        {
            DeleteFileNameToReturn = null
        };
        var storage = new FakeImageStorage();
        var service = CreateService(
            repository,
            storage,
            userId: 99);

        var result = await service.DeleteAsync(42, 9);

        Assert.False(result);
        Assert.Equal(1, repository.DeleteCallCount);
        Assert.Equal(0, storage.DeleteCallCount);
    }

    private static ImageService CreateService(
        FakeImageRepository repository,
        FakeImageStorage storage,
        int? userId,
        long maxFileSize = 5 * 1024 * 1024)
    {
        return new ImageService(
            repository,
            storage,
            new FakeCurrentUserService { UserId = userId },
            Options.Create(
                new ImageUploadOptions
                {
                    MaxFileSizeBytes = maxFileSize,
                    MaxImagesPerAnnouncement = 10,
                    UploadDirectory = "uploads"
                }));
    }

    private static UploadAnnouncementImageRequest CreateRequest(
        byte[] content,
        string contentType)
    {
        return new UploadAnnouncementImageRequest
        {
            AnnouncementId = 42,
            Content = new MemoryStream(content),
            Size = content.Length,
            ContentType = contentType
        };
    }

    private static byte[] CreateJpeg()
    {
        return
        [
            0xFF, 0xD8, 0xFF,
            0xE0, 0x00, 0x10
        ];
    }
}
