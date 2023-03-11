using Microsoft.Net.Http.Headers;

namespace SIContentService.Helpers;

/// <summary>
/// Provides helper methods for working with multipart requests.
/// </summary>
internal static class MultipartRequestHelper
{
    // Content-Type: multipart/form-data; boundary="----WebKitFormBoundarymx2fSWqWSd0OxQqq"
    // The spec says 70 characters is a reasonable limit.
    internal static string GetBoundary(MediaTypeHeaderValue contentType, int lengthLimit)
    {
        var boundary = HeaderUtilities.RemoveQuotes(contentType.Boundary).Value;

        if (string.IsNullOrWhiteSpace(boundary))
        {
            throw new InvalidDataException("Missing content-type boundary");
        }

        if (boundary.Length > lengthLimit)
        {
            throw new InvalidDataException($"Multipart boundary length limit {lengthLimit} exceeded");
        }

        return boundary;
    }

    internal static bool IsMultipartContentType(string? contentType) =>
        !string.IsNullOrEmpty(contentType) && contentType.Contains("multipart/", StringComparison.OrdinalIgnoreCase);

    // Content-Disposition: form-data; name="myfile1"; filename="Misc 002.jpg"
    internal static bool HasFileContentDisposition(ContentDispositionHeaderValue contentDisposition) =>
        contentDisposition != null
            && contentDisposition.DispositionType.Equals("form-data")
            && (!string.IsNullOrEmpty(contentDisposition.FileName.Value)
                || !string.IsNullOrEmpty(contentDisposition.FileNameStar.Value));
}
