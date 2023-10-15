namespace SIContentService.Contract.Models;

/// <summary>
/// Defines an import resource request.
/// </summary>
/// <param name="SourceUri">Resource source uri.</param>
public sealed record ImportRequest(Uri SourceUri);
