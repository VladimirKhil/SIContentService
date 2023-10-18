using Microsoft.Extensions.Options;
using SIContentService.Configuration;
using SIContentService.Contracts;

namespace SIContentService.Services;

/// <inheritdoc />
internal sealed class ResourceDownloader : IResourceDownloader
{
    private readonly HttpClient _client;
    private readonly SIContentServiceOptions _options;

    public ResourceDownloader(
        HttpClient client,
        IOptions<SIContentServiceOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    public async Task DownloadResourceAsync(Uri resourceUri, string targetPath, CancellationToken cancellationToken)
    {
        using var stream = await _client.GetStreamAsync(resourceUri, cancellationToken);
        using var targetStream = File.Create(targetPath, _options.BufferSize, FileOptions.Asynchronous);
        await stream.CopyToAsync(targetStream, _options.BufferSize, cancellationToken);
    }
}
