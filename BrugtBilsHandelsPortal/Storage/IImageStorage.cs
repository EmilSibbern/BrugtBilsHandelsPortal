namespace BrugtBilsHandelsPortal.Storage;

public interface IImageStorage
{
    Task<string> SaveAsync(
        Stream content,
        string extension,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string fileName,
        CancellationToken cancellationToken = default);

    string GetPublicUrl(string fileName);
}