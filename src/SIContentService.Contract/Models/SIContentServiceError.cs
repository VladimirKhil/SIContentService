namespace SIContentService.Contract.Models;

/// <summary>
/// Defines a SIContentService error.
/// </summary>
public sealed class SIContentServiceError
{
    /// <summary>
    /// Error code.
    /// </summary>
    public WellKnownSIContentServiceErrorCode ErrorCode { get; set; }
}
