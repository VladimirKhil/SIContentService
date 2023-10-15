namespace SIContentService.Contracts;

/// <summary>
/// Downloads resource by Uri.
/// </summary>
public interface IResourceDownloader
{
    /// <summary>
    /// Downloads resource by uri.
    /// </summary>
    /// <param name="resourceUri">Resource uri.</param>
    /// <param name="targetPath">Target path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DownloadResourceAsync(Uri resourceUri, string targetPath, CancellationToken cancellationToken);
}
