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
}
