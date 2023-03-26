using SIContentService.Contract;

namespace SIContentService.Client;

/// <inheritdoc />
internal sealed class SIContentServiceClientFactory : ISIContentServiceClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;

    public SIContentServiceClientFactory(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    public ISIContentServiceClient CreateClient(Uri? serviceUri = null)
    {
        var httpClient = _httpClientFactory.CreateClient(nameof(SIContentServiceClient));
        
        if (serviceUri != null)
        {
            httpClient.BaseAddress = serviceUri;
        }

        return new SIContentServiceClient(httpClient);
    }
}
