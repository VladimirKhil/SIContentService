using Microsoft.Extensions.Options;
using SIContentService.Configuration;
using SIContentService.Contracts;

namespace SIContentService.Services;

/// <inheritdoc />
internal sealed class ResourceDownloader : IResourceDownloader
{
    private readonly HttpClient _client;
    private readonly SIContentServiceOptions _options;
    private readonly ILogger<ResourceDownloader> _logger;

    public ResourceDownloader(
        HttpClient client,
        IOptions<SIContentServiceOptions> options,
        ILogger<ResourceDownloader> logger)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    public async Task DownloadResourceAsync(Uri resourceUri, string targetPath, CancellationToken cancellationToken)
    {
        try
        {
            using var stream = await _client.GetStreamAsync(resourceUri, cancellationToken);
            using var targetStream = File.Create(targetPath, _options.BufferSize, FileOptions.Asynchronous);
            await stream.CopyToAsync(targetStream, _options.BufferSize, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Resource {resourceUri} -> {targetPath} download error: {error}", resourceUri, targetPath, ex.Message);
            throw;
        }
    }
}
