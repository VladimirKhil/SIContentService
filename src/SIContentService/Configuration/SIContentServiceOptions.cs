namespace SIContentService.Configuration;

/// <summary>
/// Provides SIContentService configuration options.
/// </summary>
public sealed class SIContentServiceOptions
{
    public const string ConfigurationSectionName = "SIContentService";

    /// <summary>
    /// Folder for storing service content.
    /// </summary>
    public string ContentFolder { get; set; } = @".\wwwroot";

    /// <summary>
    /// Maximum package size in megabytes.
    /// </summary>
    public int MaxPackageSizeMb { get; set; } = 100;

    /// <summary>
    /// Maximum size of package with quality control in megabytes.
    /// </summary>
    public int MaxQualityPackageSizeMb { get; set; } = 150;

    /// <summary>
    /// Maximum avatar size in megabytes.
    /// </summary>
    public int MaxAvatarSizeMb { get; set; } = 1;

    /// <summary>
    /// Minimum allowed free space on storage drive in megabytes.
    /// </summary>
    public int MinDriveFreeSpaceMb { get; set; } = 7000;

    /// <summary>
    /// Minimum remaining free space on storage drive considered critical in megabytes.
    /// </summary>
    public int MinDriveCriticalSpaceMb { get; set; } = 2000;

    /// <summary>
    /// Maximum package lifetime since last usage.
    /// </summary>
    public TimeSpan MaxPackageLifetime { get; set; } = TimeSpan.FromHours(3);

    /// <summary>
    /// Maximum avatar lifetime since last usage.
    /// </summary>
    public TimeSpan MaxAvatarLifetime { get; set; } = TimeSpan.FromHours(4);

    /// <summary>
    /// Cleaning old files interval.
    /// </summary>
    public TimeSpan CleaningInterval { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Should the service serve static content by itself.
    /// </summary>
    public bool ServeStaticFiles { get; set; } = true;

    /// <summary>
    /// File operations buffer size.
    /// </summary>
    public int BufferSize { get; set; } = 81_920;
}
