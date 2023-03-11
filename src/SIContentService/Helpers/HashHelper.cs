using System.Security.Cryptography;
using System.Text;

namespace SIContentService.Helpers;

/// <summary>
/// Provides halper methods for working with hashes.
/// </summary>
internal static class HashHelper
{
    internal static string HashString(this string value) => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    internal static string EscapeHashForPath(this string hashValue) => hashValue.Replace('/', '\'');
}
