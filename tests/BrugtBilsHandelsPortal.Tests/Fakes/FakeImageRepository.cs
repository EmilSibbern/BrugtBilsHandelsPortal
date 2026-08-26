using BrugtBilsHandelsPortal.Data.Repositories.Interfaces;
using BrugtBilsHandelsPortal.DTOs.Images;
using BrugtBilsHandelsPortal.Models.Entities;

namespace BrugtBilsHandelsPortal.Tests.Fakes;

public class FakeImageRepository : IImageRepository
{
    public ImageMetadataCreateStatus AddStatus { get; set; }
        = ImageMetadataCreateStatus.Success;

    public int AddCallCount { get; private set; }
    public int GetCallCount { get; private set; }

    public int SetPrimaryCallCount { get; private set; }
    public int? LastPrimaryImageId { get; private set; }
    public bool SetPrimaryResult { get; set; } = true;
    public int DeleteCallCount { get; private set; }
    public int? LastDeletedImageId { get; private set; }
    public string? DeleteFileNameToReturn { get; set; }

    public int? LastAnnouncementId { get; private set; }
    public int? LastUserId { get; private set; }
    public string? LastFileName { get; private set; }
    public int? LastMaxImages { get; private set; }

    public List<AnnouncementImage> Images { get; } = [];

    public Task<ImageMetadataCreateResult> AddForOwnerAsync(
        int announcementId,
        int userId,
        string fileName,
        int maxImages)
    {
        AddCallCount++;
        LastAnnouncementId = announcementId;
        LastUserId = userId;
        LastFileName = fileName;
        LastMaxImages = maxImages;

        if (AddStatus != ImageMetadataCreateStatus.Success)
        {
            return Task.FromResult(
                ImageMetadataCreateResult.Failed(AddStatus));
        }

        var image = new AnnouncementImage
        {
            Id = Images.Count + 1,
            AnnouncementId = announcementId,
            FileName = fileName,
            IsPrimary = Images.Count == 0,
            SortOrder = Images.Count,
            CreatedAt = DateTime.UtcNow
        };

        Images.Add(image);

        return Task.FromResult(
            ImageMetadataCreateResult.Successful(image));
    }

    public Task<IReadOnlyList<AnnouncementImage>>
        GetForActiveAnnouncementAsync(
            int announcementId)
    {
        GetCallCount++;
        LastAnnouncementId = announcementId;

        IReadOnlyList<AnnouncementImage> result = Images
            .Where(image => image.AnnouncementId == announcementId)
            .ToArray();

        return Task.FromResult(result);
    }

    public Task<bool> SetPrimaryForOwnerAsync(
        int announcementId,
        int imageId,
        int userId)
    {
        SetPrimaryCallCount++;
        LastAnnouncementId = announcementId;
        LastPrimaryImageId = imageId;
        LastUserId = userId;

        if (SetPrimaryResult)
        {
            foreach (var image in Images.Where(
                         image =>
                             image.AnnouncementId == announcementId))
            {
                image.IsPrimary = image.Id == imageId;
            }
        }

        return Task.FromResult(SetPrimaryResult);
    }

    public Task<string?> DeleteForOwnerAsync(
        int announcementId,
        int imageId,
        int userId)
    {
        DeleteCallCount++;
        LastAnnouncementId = announcementId;
        LastDeletedImageId = imageId;
        LastUserId = userId;
        return Task.FromResult(DeleteFileNameToReturn);
    }
}
