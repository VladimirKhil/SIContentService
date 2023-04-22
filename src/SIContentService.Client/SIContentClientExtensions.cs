using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Polly;
using Polly.Extensions.Http;
using SIContentService.Contract;
using System.Net;
using System.Net.Http.Handlers;
using System.Net.Http.Headers;
using System.Text;

namespace SIContentService.Client;

/// <summary>
/// Provides an extension method for adding <see cref="ISIContentServiceClient" /> implementation to service collection.
/// </summary>
public static class SIContentClientExtensions
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
                if (options != null)
                {
                    client.BaseAddress = options.ServiceUri;
                    client.Timeout = options.Timeout;

                    SetAuthSecret(options, client);
                }
                
                client.DefaultRequestVersion = HttpVersion.Version20;
                
            })
            .AddPolicyHandler(
                HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .WaitAndRetryAsync(
                        options?.RetryCount ?? SIContentClientOptions.DefaultRetryCount,
                        retryAttempt => TimeSpan.FromSeconds(Math.Pow(1.5, retryAttempt))));

        services.AddSingleton<ISIContentServiceClientFactory, SIContentServiceClientFactory>();

        return services;
    }

    /// <summary>
    /// Allows to create custom SIContentService client with upload progress support.
    /// </summary>
    /// <remarks>
    /// This method is intended to be used in desktop client app code only.
    /// </remarks>
    /// <param name="options">Client options.</param>
    /// <param name="onUploadProgress">Upload progress callback.</param>
    public static ISIContentServiceClient CreateSIContentServiceClient(SIContentClientOptions options, Action<int>? onUploadProgress = null)
    {
        var cookieContainer = new CookieContainer();
        HttpMessageHandler handler = new HttpClientHandler { CookieContainer = cookieContainer };

        if (onUploadProgress != null)
        {
            // TODO: think about switching to reporting progress in StreamContent class overload
            // That would allow to use normal DI to access SIContentServiceClient instances
            var progressHandler = new ProgressMessageHandler(handler);
            progressHandler.HttpSendProgress += (sender, e) => onUploadProgress(e.ProgressPercentage);

            handler = progressHandler;
        }

        var policyHandler = new PolicyHttpMessageHandler(
            HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(
                    options.RetryCount,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(1.5, retryAttempt))))
        {
            InnerHandler = handler
        };

        var client = new HttpClient(policyHandler)
        {
            BaseAddress = options.ServiceUri,
            Timeout = options.Timeout,
            DefaultRequestVersion = HttpVersion.Version20
        };

        SetAuthSecret(options, client);

        return new SIContentServiceClient(client);
    }

    private static void SetAuthSecret(SIContentClientOptions options, HttpClient client)
    {
        if (options.ClientSecret == null)
        {
            return;
        }

        var authHeader = Convert.ToBase64String(Encoding.ASCII.GetBytes($"admin:{options.ClientSecret}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
    }
}
