using SIContentService.Contract;
using System.Net.Http.Headers;
using System.Text;

namespace SIContentService.Client;

/// <inheritdoc />
internal sealed class SIContentServiceClientFactory : ISIContentServiceClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;

    public SIContentServiceClientFactory(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    public ISIContentServiceClient CreateClient(Uri? serviceUri = null, string? clientSecret = null)
    {
        var httpClient = _httpClientFactory.CreateClient(nameof(ISIContentServiceClient));
        
        if (serviceUri != null)
        {
            httpClient.BaseAddress = serviceUri;
        }

        if (clientSecret != null)
        {
            var authHeader = Convert.ToBase64String(Encoding.ASCII.GetBytes($"admin:{clientSecret}"));
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
        }

        return new SIContentServiceClient(httpClient);
    }
}
