using AspNetCoreRateLimit;
using Serilog;
using SIContentService.Configuration;
using SIContentService.Contracts;
using SIContentService.Middlewares;
using SIContentService.Services;
using SIContentService.Services.Background;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc
    .WriteTo.Console()
    .WriteTo.File(
        "logs/sicontent.log",
        fileSizeLimitBytes: 5 * 1024 * 1024,
        shared: true,
        rollOnFileSizeLimit: true,
        retainedFileCountLimit: 5,
        flushToDiskInterval: TimeSpan.FromSeconds(1))
    .ReadFrom.Configuration(ctx.Configuration)); // Not working when Assembly is trimmed

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
}

static void Configure(WebApplication app)
{
    app.UseMiddleware<ErrorHandlingMiddleware>();

    app.UseRouting();
    app.MapControllers();

    app.UseIpRateLimiting();
}

static void AddRateLimits(IServiceCollection services, IConfiguration configuration)
{
    services.Configure<IpRateLimitOptions>(configuration.GetSection("IpRateLimit"));

    services.AddMemoryCache();
    services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
    services.AddInMemoryRateLimiting();
}