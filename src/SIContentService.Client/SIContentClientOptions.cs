namespace SIContentService.Client;

/// <summary>
/// Provides options for <see cref="SIContentServiceClient" /> class.
/// </summary>
public sealed class SIContentClientOptions
{
    /// <summary>
    /// Name of the configuration section holding these options.
    /// </summary>
    public const string ConfigurationSectionName = "SIContentServiceClient";

    public const int DefaultRetryCount = 3;

    /// <summary>
    /// SIContent service Uri.
    /// </summary>
    public Uri? ServiceUri { get; set; }

    /// <summary>
    /// Retry count policy.
    /// </summary>
    public int RetryCount { get; set; } = DefaultRetryCount;
}
