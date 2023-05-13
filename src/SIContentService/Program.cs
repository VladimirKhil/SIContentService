using AspNetCoreRateLimit;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Serilog;
using SIContentService.Configuration;
using SIContentService.Contracts;
using SIContentService.Helpers;
using SIContentService.Metrics;
using SIContentService.Middlewares;
using SIContentService.Services;
using SIContentService.Services.Background;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration));

ConfigureServices(builder.Services, builder.Configuration);

var app = builder.Build();

app.UseSerilogRequestLogging();

Configure(app);

app.Run();

static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    services.Configure<SIContentServiceOptions>(configuration.GetSection(SIContentServiceOptions.ConfigurationSectionName));

    services.AddControllers();

    services.AddSingleton<IStorageService, StorageService>();
    services.AddSingleton<IPackageService, PackageService>();
    services.AddSingleton<IAvatarService, AvatarService>();

    services.AddHostedService<CleanerService>();

    AddRateLimits(services, configuration);
    AddMetrics(services);
}

static void Configure(WebApplication app)
{
    var options = app.Services.GetRequiredService<IOptions<SIContentServiceOptions>>().Value;

    app.UseMiddleware<ErrorHandlingMiddleware>();

    if (options.ServeStaticFiles)
    {
        var contentPath = StringHelper.BuildRootedPath(options.ContentFolder);
        Directory.CreateDirectory(contentPath);
        app.UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(contentPath) });
    }

    app.UseRouting();
    app.MapControllers();

    app.UseIpRateLimiting();

    app.UseOpenTelemetryPrometheusScrapingEndpoint();
}

static void AddRateLimits(IServiceCollection services, IConfiguration configuration)
{
    services.Configure<IpRateLimitOptions>(configuration.GetSection("IpRateLimit"));

    services.AddMemoryCache();
    services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
    services.AddInMemoryRateLimiting();
}

static void AddMetrics(IServiceCollection services)
{
    var meters = new OtelMetrics();

    services.AddOpenTelemetry().WithMetrics(builder =>
        builder
            .ConfigureResource(rb => rb.AddService("SIContent"))
            .AddMeter(meters.MeterName)
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation()
            .AddPrometheusExporter());

    services.AddSingleton(meters);
}