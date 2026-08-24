using BrugtBilsHandelsPortal.Configuration;
using Microsoft.Extensions.Options;

namespace BrugtBilsHandelsPortal.Storage;

/// <summary>Gemmer annoncebilleder lokalt og beskytter filsystemet mod usikre filnavne.</summary>
public class LocalImageStorage : IImageStorage
{
    private readonly string _uploadDirectory;
    private readonly string _publicDirectory;

    public LocalImageStorage(
        IWebHostEnvironment environment,
        IOptions<ImageUploadOptions> options)
    {
        var webRoot = environment.WebRootPath;

        if (string.IsNullOrWhiteSpace(webRoot))
        {
            throw new InvalidOperationException(
                "WebRootPath er ikke konfigureret.");
        }

        _publicDirectory =
            options.Value.UploadDirectory.Trim(
                '/',
                '\\');

        if (string.IsNullOrWhiteSpace(_publicDirectory))
        {
            throw new InvalidOperationException(
                "UploadDirectory er ikke konfigureret.");
        }

        _uploadDirectory = Path.GetFullPath(
            Path.Combine(
                webRoot,
                _publicDirectory));

        var normalizedWebRoot =
            Path.GetFullPath(webRoot)
                .TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        if (!_uploadDirectory.StartsWith(
                normalizedWebRoot,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "UploadDirectory skal ligge under wwwroot.");
        }

        Directory.CreateDirectory(_uploadDirectory);
    }

    public async Task<string> SaveAsync(
        Stream content,
        string extension,
        CancellationToken cancellationToken = default)
    {
        var normalizedExtension =
            extension.TrimStart('.').ToLowerInvariant();

        if (normalizedExtension is not
            ("jpg" or "png" or "webp"))
        {
            throw new ArgumentException(
                "Filtypen er ikke tilladt.",
                nameof(extension));
        }

        var fileName =
            $"{Guid.NewGuid():N}.{normalizedExtension}";

        var fullPath = GetSafePath(fileName);

        await using var destination = new FileStream(
            fullPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        await content.CopyToAsync(
            destination,
            cancellationToken);

        return fileName;
    }

    public Task DeleteAsync(
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var fullPath = GetSafePath(fileName);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    public string GetPublicUrl(string fileName)
    {
        var safeFileName =
            Path.GetFileName(fileName);

        return $"/{_publicDirectory}/" +
               Uri.EscapeDataString(safeFileName);
    }

    // Path.GetFileName fjerner eventuelle mappesegmenter og forhindrer path traversal.
    private string GetSafePath(string fileName)
    {
        var safeFileName = Path.GetFileName(fileName);

        if (!string.Equals(
                fileName,
                safeFileName,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Filnavnet er ugyldigt.",
                nameof(fileName));
        }

        var fullPath = Path.GetFullPath(
            Path.Combine(
                _uploadDirectory,
                safeFileName));

        var normalizedDirectory =
            _uploadDirectory.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(
                normalizedDirectory,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Filstien ligger uden for uploadmappen.");
        }

        return fullPath;
    }
}
