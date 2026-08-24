namespace BrugtBilsHandelsPortal.Storage;

/// <summary>Abstraherer den fysiske billedlagring fra service- og databaselagene.</summary>
public interface IImageStorage
{
    /// <summary>Gemmer en billedstream under et servergenereret filnavn.</summary>
    Task<string> SaveAsync(
        Stream content,
        string extension,
        CancellationToken cancellationToken = default);

    /// <summary>Sletter en tidligere gemt fil, hvis den findes.</summary>
    Task DeleteAsync(
        string fileName,
        CancellationToken cancellationToken = default);

    /// <summary>Omsætter et internt filnavn til den offentlige URL, som klienten kan vise.</summary>
    string GetPublicUrl(string fileName);
}
