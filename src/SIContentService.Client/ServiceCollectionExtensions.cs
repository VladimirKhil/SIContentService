using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using SIContentService.Contract;
using System.Net;

namespace SIContentService.Client;

/// <summary>
/// Provides an extension method for adding <see cref="ISIContentServiceClient" /> implementation to service collection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds <see cref="ISIContentServiceClient" /> implementation to service collection.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">App configuration.</param>
    public static IServiceCollection AddSIContentServiceClient(this IServiceCollection services, IConfiguration configuration)
    {
        var optionsSection = configuration.GetSection(SIContentClientOptions.ConfigurationSectionName);
        services.Configure<SIContentClientOptions>(optionsSection);

        var options = optionsSection.Get<SIContentClientOptions>();

        services.AddHttpClient<ISIContentServiceClient, SIContentServiceClient>(
            client =>
            {
                client.BaseAddress = options?.ServiceUri;
                client.DefaultRequestVersion = HttpVersion.Version20;
            }).AddPolicyHandler(HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(
                    options?.RetryCount ?? SIContentClientOptions.DefaultRetryCount,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(1.5, retryAttempt))));

        return services;
    }
}
