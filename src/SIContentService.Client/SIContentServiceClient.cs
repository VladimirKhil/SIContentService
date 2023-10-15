using SIContentService.Client.Helpers;
using SIContentService.Client.Properties;
using SIContentService.Contract;
using SIContentService.Contract.Helpers;
using SIContentService.Contract.Models;
using System.Net;

namespace SIContentService.Client;

/// <inheritdoc cref="ISIContentServiceClient" />
internal sealed class SIContentServiceClient : ISIContentServiceClient
{
    private const int BufferSize = 80 * 1024;

    private readonly HttpClient _client;

    public Uri? ServiceUri => _client.BaseAddress;

    public IImportApi Import { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="SIContentServiceClient" /> class.
    /// </summary>
    /// <param name="client">HTTP client to use.</param>
    public SIContentServiceClient(HttpClient client)
    {
        _client = client;
        Import = new ImportApi(client);
    }

    public async Task<Uri?> TryGetAvatarUriAsync(FileKey avatarKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var avatarHash = Base64Helper.EscapeBase64(Convert.ToBase64String(avatarKey.Hash));
            var avatarName = Uri.EscapeDataString(avatarKey.Name);

            return new Uri(
                await _client.GetStringAsync(
                    $"{HttpHelper.ApiPrefix}content/avatars/{avatarHash}/{avatarName}",
                    cancellationToken),
                UriKind.RelativeOrAbsolute);
        }
        catch (HttpRequestException exc) when (exc.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<Uri?> TryGetPackageUriAsync(FileKey packageKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var packageHash = Base64Helper.EscapeBase64(Convert.ToBase64String(packageKey.Hash));
            var packageName = Uri.EscapeDataString(packageKey.Name);

            return new Uri(
                await _client.GetStringAsync(
                    $"{HttpHelper.ApiPrefix}content/packages/{packageHash}/{packageName}",
                    cancellationToken),
                UriKind.RelativeOrAbsolute);
        }
        catch (HttpRequestException exc) when (exc.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public Task<Uri> UploadAvatarAsync(FileKey avatarKey, Stream avatarStream, CancellationToken cancellationToken = default) =>
        UploadContentAsync($"{HttpHelper.ApiPrefix}content/avatars", avatarKey, avatarStream, cancellationToken);

    public Task<Uri> UploadPackageAsync(FileKey packageKey, Stream packageStream, CancellationToken cancellationToken = default) =>
        UploadContentAsync($"{HttpHelper.ApiPrefix}content/packages", packageKey, packageStream, cancellationToken);

    public Task<HttpResponseMessage> GetAsync(string requestUri, CancellationToken cancellationToken = default) =>
        _client.GetAsync(RemoveLeadingSlash(requestUri), cancellationToken);

    public void Dispose() => _client.Dispose();

    private static string? RemoveLeadingSlash(string requestUri) => requestUri.StartsWith('/') ? requestUri[1..] : requestUri;

    private async Task<Uri> UploadContentAsync(
        string contentUri,
        FileKey contentKey,
        Stream contentStream,
        CancellationToken cancellationToken = default)
    {
        using var content = new StreamContent(contentStream, BufferSize);

        try
        {
            using var formData = new MultipartFormDataContent
            {
                { content, "file", contentKey.Name }
            };

            formData.Headers.ContentMD5 = contentKey.Hash;
            using var response = await _client.PostAsync(contentUri, formData, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await HttpHelper.GetErrorMessageAsync(response, cancellationToken);
                throw new Exception(errorMessage);
            }

            return new Uri(await response.Content.ReadAsStringAsync(cancellationToken), UriKind.RelativeOrAbsolute);
        }
        catch (HttpRequestException exc)
        {
            throw new Exception(Resources.UploadFileConnectionError, exc.InnerException ?? exc);
        }
        catch (TaskCanceledException exc)
        {
            if (!exc.CancellationToken.IsCancellationRequested)
            {
                throw new Exception(Resources.UploadFileTimeout, exc);
            }

            throw exc;
        }
    }
}
