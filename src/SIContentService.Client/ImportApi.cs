using SIContentService.Client.Helpers;
using SIContentService.Contract;
using SIContentService.Contract.Models;
using System.Net.Http.Json;

namespace SIContentService.Client;

/// <inheritdoc />
internal sealed class ImportApi : IImportApi
{
    private readonly HttpClient _client;

    public ImportApi(HttpClient client) => _client = client;

    public async Task<Uri> ImportPackageAsync(Uri sourceUri, CancellationToken cancellationToken = default)
    {
        var response = await HttpClientJsonExtensions.PutAsJsonAsync(
            _client,
            $"{HttpHelper.ApiPrefix}import/packages",
            new ImportRequest(sourceUri),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await HttpHelper.GetErrorMessageAsync(response, cancellationToken);
            throw new Exception(errorMessage);
        }

        return new Uri(await response.Content.ReadAsStringAsync(cancellationToken), UriKind.RelativeOrAbsolute);
    }
}