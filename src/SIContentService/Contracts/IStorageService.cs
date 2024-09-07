namespace SIContentService.Contracts;

/// <summary>
/// Provides global methods for working with storage.
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Checks if storage has enough free space.
    /// </summary>
    bool CheckFreeSpace();

    /// <summary>
    /// Checks if the remaing free space has a critical value.
    /// </summary>
    bool IsFreeSpaceCritical();

    /// <summary>
    /// Valides package file.
    /// </summary>
    /// <param name="filePath">Package file path.</param>
    /// <param name="hasQualityControl">Indicates if quality control is enabled.</param>
    void ValidatePackageFile(string filePath, bool hasQualityControl);
}
