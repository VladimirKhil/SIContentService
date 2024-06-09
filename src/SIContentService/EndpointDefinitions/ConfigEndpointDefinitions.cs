namespace SIContentService.EndpointDefinitions;

/// <summary>
/// Provides information about application configuration.
/// </summary>
internal static class ConfigEndpointDefinitions
{
    public static void DefineContentEndpoint(WebApplication app)
    {
        app.MapGet("/config", (IConfiguration configuration) =>
        {
            return Results.Ok(configuration.AsEnumerable().ToDictionary(p => p.Key, p => p.Value));
        });
    }
}
