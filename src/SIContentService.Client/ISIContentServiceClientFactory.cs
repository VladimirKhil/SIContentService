using SIContentService.Contract;

namespace SIContentService.Client;

/// <summary>
/// Provides method for creating custom SIContentService clients.
/// </summary>
public interface ISIContentServiceClientFactory
{
    /// <summary>
    /// Creates SIContentService client with custom service uri.
    /// </summary>
    /// <param name="serviceUri">Service uri.</param>
    /// <param name="clientSecret">Optional client secret.</param>
    ISIContentServiceClient CreateClient(Uri? serviceUri = null, string? clientSecret = null);
}
