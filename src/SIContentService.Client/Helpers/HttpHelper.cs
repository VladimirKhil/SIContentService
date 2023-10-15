using SIContentService.Client.Properties;
using SIContentService.Contract.Models;
using System.Net;
using System.Text.Json;

namespace SIContentService.Client.Helpers;

/// <summary>
/// Provides helper methods for working with HTTP requests and responses.
/// </summary>
internal static class HttpHelper
{
    private const string RequestBodyTooLargeError = "Request body too large.";
    internal const string ApiPrefix = "api/v1/";

    internal static async Task<string> GetErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
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
