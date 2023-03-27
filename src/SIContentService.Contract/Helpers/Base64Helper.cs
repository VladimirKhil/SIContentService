namespace SIContentService.Contract.Helpers;

/// <summary>
/// Provides helper methods for making Base64 string URI and path-friendly.
/// </summary>
public static class Base64Helper
{
    /// <summary>
    /// Escapes Base64 string.
    /// </summary>
    /// <param name="base64value">Base64 string.</param>
    /// <returns>Escaped string.</returns>
    public static string EscapeBase64(string base64value) => base64value.Replace('/', '_').Replace('+', '-').Replace("=", "");
}
