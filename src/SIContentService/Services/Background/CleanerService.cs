using Microsoft.Extensions.Options;
using SIContentService.Configuration;
using SIContentService.Contracts;

namespace SIContentService.Services.Background;

/// <summary>
/// Periodically cleans old packages and avatars.
/// </summary>
public sealed class CleanerService : BackgroundService
{
    private readonly IPackageService _packageService;
    private readonly IAvatarService _avatarService;
    private readonly SIContentServiceOptions _options;
    private readonly ILogger<CleanerService> _logger;

    public CleanerService(
        IPackageService packageService,
        IAvatarService avatarService,
        IOptions<SIContentServiceOptions> options,
        ILogger<CleanerService> logger)
    {
        _packageService = packageService;
        _avatarService = avatarService;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Cleaning service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            await _packageService.ClearOldPackagesAsync(stoppingToken);
            await _avatarService.ClearOldAvatarsAsync(stoppingToken);

            await Task.Delay(_options.CleaningInterval, stoppingToken);
        }

        _logger.LogInformation("Cleaning service stopped");
    }
}
