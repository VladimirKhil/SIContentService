namespace SIContentService.Helpers;

/// <summary>
/// Provides helper method for working with strings.
/// </summary>
internal static class StringHelper
{
    /// <summary>
    /// Removes quotes around the value.
    /// </summary>
    /// <param name="value">Value to process.</param>
    /// <returns>Original value or value without surrounding quotes.</returns>
    internal static string UnquoteValue(string value)
    {
        if (value.Length > 1 && value.StartsWith("\"") && value.EndsWith("\""))
        {
            value = value[1..^1];
        }

        return value;
    }
}
