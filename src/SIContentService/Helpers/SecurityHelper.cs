using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace SIContentService.Helpers;

/// <summary>
/// Provides halper methods for working with user data.
/// </summary>
internal static class SecurityHelper
{
    internal static string HashString(this string value) => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    internal static string EscapeHashForPath(this string hashValue) => hashValue.Replace('/', '\'');

    internal static string GetSafeExtension(string extension) => Regex.Replace(extension, "[^.a-zA-Z]+", "");
}