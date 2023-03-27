using SIContentService.Client.Properties;
using SIContentService.Contract;
using SIContentService.Contract.Helpers;
using SIContentService.Contract.Models;
using System.Net;
using System.Text.Json;

namespace SIContentService.Client;

/// <inheritdoc cref="ISIContentServiceClient" />
internal sealed class SIContentServiceClient : ISIContentServiceClient
{
    private const int BufferSize = 80 * 1024;
    private const string RequestBodyTooLargeError = "Request body too large.";
    private const string ApiPrefix = "api/v1/";

    private readonly HttpClient _client;

    /// <summary>
    /// Initializes a new instance of <see cref="SIContentServiceClient" /> class.
    /// </summary>
    /// <param name="client">HTTP client to use.</param>
    public SIContentServiceClient(HttpClient client) => _client = client;

    public async Task<string?> TryGetAvatarUriAsync(FileKey avatarKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var avatarHash = Base64Helper.EscapeBase64(Convert.ToBase64String(avatarKey.Hash));
            var avatarName = Uri.EscapeDataString(avatarKey.Name);

            return await _client.GetStringAsync(
                $"{ApiPrefix}content/avatars/{avatarHash}/{avatarName}",
                cancellationToken);
        }
        catch (HttpRequestException exc) when (exc.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<string?> TryGetPackageUriAsync(FileKey packageKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var packageHash = Base64Helper.EscapeBase64(Convert.ToBase64String(packageKey.Hash));
            var packageName = Uri.EscapeDataString(packageKey.Name);

            return await _client.GetStringAsync(
                $"{ApiPrefix}content/packages/{packageHash}/{packageName}",
                cancellationToken);
        }
        catch (HttpRequestException exc) when (exc.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public Task<string> UploadAvatarAsync(FileKey avatarKey, Stream avatarStream, CancellationToken cancellationToken = default) =>
        UploadContentAsync($"{ApiPrefix}content/avatars", avatarKey, avatarStream, cancellationToken);

    public Task<string> UploadPackageAsync(FileKey packageKey, Stream packageStream, CancellationToken cancellationToken = default) =>
        UploadContentAsync($"{ApiPrefix}content/packages", packageKey, packageStream, cancellationToken);

    public Task<HttpResponseMessage> GetAsync(string requestUri, CancellationToken cancellationToken = default) =>
        _client.GetAsync(RemoveLeadingSlash(requestUri), cancellationToken);

    public void Dispose() => _client.Dispose();

    private static string? RemoveLeadingSlash(string requestUri) => requestUri.StartsWith('/') ? requestUri[1..] : requestUri;

    private async Task<string> UploadContentAsync(
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
                var errorMessage = await GetErrorMessageAsync(response, cancellationToken);
                throw new Exception(errorMessage);
            }

            return await response.Content.ReadAsStringAsync(cancellationToken);
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

    private static async Task<string> GetErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var serverError = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode == HttpStatusCode.RequestEntityTooLarge
            || response.StatusCode == HttpStatusCode.BadRequest && serverError == RequestBodyTooLargeError)
        {
            return Resources.FileTooLarge;
        }

        if (response.StatusCode == HttpStatusCode.BadGateway)
        {
            return $"{response.StatusCode}: Bad Gateway";
        }

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            return $"{response.StatusCode}: Too many requests. Try again later";
        }

        try
        {
            var error = JsonSerializer.Deserialize<SIContentServiceError>(serverError);

            if (error != null)
            {
                return error.ErrorCode.ToString();
            }
        }
        catch // Invalid JSON or wrong type
        {

        }

        return $"{response.StatusCode}: {serverError}";
    }
}
