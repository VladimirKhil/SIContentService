using SIContentService.Contracts;

namespace SIContentService.Services;

/// <inheritdoc />
internal sealed class ResourceDownloader : IResourceDownloader
{
    private readonly HttpClient _client;

    public ResourceDownloader(HttpClient client) => _client = client;

    public async Task DownloadResourceAsync(Uri resourceUri, string targetPath, CancellationToken cancellationToken)
    {
        using var stream = await _client.GetStreamAsync(resourceUri, cancellationToken);
        using var targetStream = File.Create(targetPath);
        await stream.CopyToAsync(targetStream, cancellationToken);
    }
}
