using SIContentService.Contract.Models;

namespace SIContentService.Contract;

/// <summary>
/// Defines a SIContentService client.
/// </summary>
public interface ISIContentServiceClient
{
    /// <summary>
    /// Gets package public uri.
    /// </summary>
    /// <param name="packageKey">Unqiue package key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<string?> TryGetPackageUriAsync(FileKey packageKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads package to service.
    /// </summary>
    /// <param name="packageKey">Unqiue package key.</param>
    /// <param name="packageStream">Package data stream.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<string> UploadPackageAsync(FileKey packageKey, Stream packageStream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets avatar public uri.
    /// </summary>
    /// <param name="avatarKey">Unqiue avatar key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<string?> TryGetAvatarUriAsync(FileKey avatarKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads avatar to service.
    /// </summary>
    /// <param name="avatarKey">Unqiue avatar key.</param>
    /// <param name="avatarStream">Avatar data stream.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<string> UploadAvatarAsync(FileKey avatarKey, Stream avatarStream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets resource by Uri.
    /// </summary>
    /// <param name="requestUri">Resource Uri.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<HttpResponseMessage> GetAsync(string requestUri, CancellationToken cancellationToken = default);
}
