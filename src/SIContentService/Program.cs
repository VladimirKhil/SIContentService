using AspNetCoreRateLimit;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Serilog;
using SIContentService.Configuration;
using SIContentService.Contracts;
using SIContentService.Helpers;
using SIContentService.Middlewares;
using SIContentService.Services;
using SIContentService.Services.Background;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc
#if !DEBUG
    .WriteTo.Console()
    .WriteTo.File(
        "logs/sicontent.log",
        fileSizeLimitBytes: 5 * 1024 * 1024,
        shared: true,
        rollOnFileSizeLimit: true,
        retainedFileCountLimit: 5,
        flushToDiskInterval: TimeSpan.FromSeconds(1))
#endif
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
}

static void AddRateLimits(IServiceCollection services, IConfiguration configuration)
{
    services.Configure<IpRateLimitOptions>(configuration.GetSection("IpRateLimit"));

    services.AddMemoryCache();
    services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
    services.AddInMemoryRateLimiting();
}