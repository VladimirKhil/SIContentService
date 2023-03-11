namespace SIContentService.Models;

/// <summary>
/// Contains information about uploaded content.
/// </summary>
public sealed class ContentInfo
{
    /// <summary>
    /// Last content usage time.
    /// </summary>
    public DateTimeOffset LastUsageTime { get; set; }
}
