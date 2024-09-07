using Microsoft.Extensions.Options;
using SIContentService.Configuration;
using SIContentService.Contracts;

namespace SIContentService.Services;

/// <inheritdoc />
internal sealed class ResourceDownloader(
    HttpClient client,
    IOptions<SIContentServiceOptions> options,
    ILogger<ResourceDownloader> logger) : IResourceDownloader
{
    private readonly SIContentServiceOptions _options = options.Value;

    public async Task DownloadResourceAsync(Uri resourceUri, string targetPath, CancellationToken cancellationToken)
    {
        try
        {
            using var stream = await client.GetStreamAsync(resourceUri, cancellationToken);
            using var targetStream = File.Create(targetPath, _options.BufferSize, FileOptions.Asynchronous);
            await stream.CopyToAsync(targetStream, _options.BufferSize, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Resource {resourceUri} -> {targetPath} download error: {error}", resourceUri, targetPath, ex.Message);
            throw;
        }
    }
}
