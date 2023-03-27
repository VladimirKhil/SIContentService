using SIContentService.Contract.Helpers;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace SIContentService.Helpers;

/// <summary>
/// Provides halper methods for working with user data.
/// </summary>
public static partial class SecurityHelper
{
    private const int ExtensionMaxLength = 4;

    [GeneratedRegex("[^a-zA-Z]+")]
    private static partial Regex OnlyCharsRegex();

    internal static string HashString(this string value) =>
        Base64Helper.EscapeBase64(Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(value))));

    /// <summary>
    /// Provides file extension that is not too long and does not contain special symbols.
    /// </summary>
    /// <param name="path">File path.</param>
    /// <returns>Safe file extension.</returns>
    public static string GetSafeExtension(string path)
    {
        var extension = Path.GetExtension(path);
        var maxLength = Math.Min(ExtensionMaxLength, extension.Length - 1);

        return '.' + OnlyCharsRegex().Replace(extension.Substring(1, maxLength), "");
    }
}