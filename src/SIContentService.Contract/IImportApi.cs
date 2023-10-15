namespace SIContentService.Contract;

/// <summary>
/// Provides API for importing content from external source.
/// </summary>
public interface IImportApi
{
    /// <summary>
    /// Imports package from external source.
    /// </summary>
    /// <param name="sourceUri">External source uri.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Uploaded package uri.</returns>
    Task<Uri> ImportPackageAsync(Uri sourceUri, CancellationToken cancellationToken = default);
}
