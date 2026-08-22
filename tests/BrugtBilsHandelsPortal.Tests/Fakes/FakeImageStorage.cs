using BrugtBilsHandelsPortal.Storage;

namespace BrugtBilsHandelsPortal.Tests.Fakes;

public class FakeImageStorage : IImageStorage
{
    public int SaveCallCount { get; private set; }
    public int DeleteCallCount { get; private set; }

    public string FileNameToReturn { get; set; }
        = "generated-image.jpg";

    public string? LastExtension { get; private set; }
    public byte[]? LastSavedContent { get; private set; }
    public string? LastDeletedFileName { get; private set; }

    public async Task<string> SaveAsync(
        Stream content,
        string extension,
        CancellationToken cancellationToken = default)
    {
        SaveCallCount++;
        LastExtension = extension;

        await using var copy = new MemoryStream();
        await content.CopyToAsync(copy, cancellationToken);
        LastSavedContent = copy.ToArray();

        return FileNameToReturn;
    }

    public Task DeleteAsync(
        string fileName,
        CancellationToken cancellationToken = default)
    {
        DeleteCallCount++;
        LastDeletedFileName = fileName;

        return Task.CompletedTask;
    }

    public string GetPublicUrl(string fileName)
    {
        return $"/uploads/{fileName}";
    }
}
