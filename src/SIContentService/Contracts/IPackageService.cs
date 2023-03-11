namespace SIContentService.Contracts;

/// <summary>
/// Provides API for working with packages.
/// </summary>
public interface IPackageService
{
    /// <summary>
    /// Loads user package into the storage.
    /// </summary>
    /// <param name="filePath">Package file path.</param>
    /// <param name="packageName">Package name.</param>
    /// <param name="packageHashString">Package hash.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<string> ImportUserPackageAsync(
        string filePath,
        string packageName,
        string packageHashString,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets package path.
    /// </summary>
    /// <param name="packageName">Package name.</param>
    /// <param name="packageHashString">Package hash.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<string?> TryGetPackagePathAsync(string packageName, string packageHashString, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes old packages.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ClearOldPackagesAsync(CancellationToken cancellationToken);
}
