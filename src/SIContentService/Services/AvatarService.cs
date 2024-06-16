using Microsoft.Extensions.Options;
using SIContentService.Configuration;
using SIContentService.Contracts;
using SIContentService.Helpers;
using SIContentService.Metrics;
using SIContentService.Models;
using System.Text.Json;

namespace SIContentService.Services;

/// <inheritdoc cref="IAvatarService" />
public sealed class AvatarService : IAvatarService
{
    private const string InfoFolderName = ".info";

    private readonly SIContentServiceOptions _options;
    private readonly OtelMetrics _metrics;
    private readonly ILogger<AvatarService> _logger;
    private readonly string _rootFolder;

    private readonly CollectionLocker _locker = new();

    public AvatarService(
        IOptions<SIContentServiceOptions> options,
        OtelMetrics metrics,
        ILogger<AvatarService> logger)
    {
        _options = options.Value;
        _metrics = metrics;
        _logger = logger;

        _rootFolder = Path.Combine(StringHelper.BuildRootedPath(options.Value.ContentFolder), "avatars");
    }

    public async Task<string> AddAvatarAsync(string avatarName, string avatarHashString, Func<Stream, Task> fileWriteAsync)
    {
        var avatarPath = BuildAvatarPath(avatarName, avatarHashString);

        _logger.LogInformation("Adding avatar. Name: {name}, hash: {hash}, path: {path}", avatarName, avatarHashString, avatarPath);

        if (File.Exists(avatarPath))
        {
            return avatarPath;
        }

        Directory.CreateDirectory(_rootFolder);

        await _locker.DoAsync(
            avatarPath,
            async () =>
            {
                using var fs = File.Create(avatarPath);

                // Do not remove await because fs would be disposed after return
                await fileWriteAsync(fs);
            });

        _metrics.AddAvatar();

        return avatarPath;
    }

    public Task<string?> TryGetAvatarPathAsync(string avatarName, string avatarHashString, CancellationToken cancellationToken = default)
    {
        var avatarPath = BuildAvatarPath(avatarName, avatarHashString);

        _logger.LogInformation("Getting avatar. Name: {name}, hash: {hash}, path: {path}", avatarName, avatarHashString, avatarPath);

        return UpdateAvatarUsageAsync(avatarPath, cancellationToken);
    }

    public async Task ClearOldAvatarsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var now = DateTimeOffset.UtcNow;

            var dirInfo = new DirectoryInfo(_rootFolder);

            if (!dirInfo.Exists)
            {
                return;
            }

            foreach (var file in dirInfo.EnumerateFiles())
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                await _locker.DoWithLockAsync(
                    file.FullName,
                    () =>
                    {
                        var infoFile = Path.Combine(_rootFolder, InfoFolderName, file.Name + ".json");

                        DateTimeOffset lastUsageTime;

                        if (File.Exists(infoFile))
                        {
                            try
                            {
                                var info = File.ReadAllText(infoFile);
                                var contentInfo = JsonSerializer.Deserialize(info, ContentInfoContext.Default.ContentInfo);
                                lastUsageTime = contentInfo?.LastUsageTime ?? file.CreationTime;
                            }
                            catch (Exception exc)
                            {
                                _logger.LogWarning(
                                    exc,
                                    "Getting last usage time from file {fileName} resulted in error: {error}",
                                    infoFile,
                                    exc.Message);

                                lastUsageTime = file.CreationTime;
                            }
                        }
                        else
                        {
                            lastUsageTime = file.CreationTime;
                        }

                        if (now.Subtract(lastUsageTime) < _options.MaxAvatarLifetime)
                        {
                            return;
                        }

                        try
                        {
                            file.Delete();

                            if (File.Exists(infoFile))
                            {
                                File.Delete(infoFile);
                            }

                            _metrics.DeleteAvatar();
                        }
                        catch (Exception exc)
                        {
                            _logger.LogError(exc, "Error deleting file {fileName}: {error}", file.FullName, exc.Message);
                        }
                    },
                    cancellationToken);
            }
        }
        catch (Exception exc)
        {
            _logger.LogError(exc, "Clear old avatars error: {error}", exc.Message);
        }
    }

    private Task<string?> UpdateAvatarUsageAsync(string avatarPath, CancellationToken cancellationToken = default) =>
        _locker.DoWithLockAsync(
            avatarPath,
            () =>
            {
                if (!File.Exists(avatarPath))
                {
                    return null;
                }

                try
                {
                    var contentInfo = new ContentInfo { LastUsageTime = DateTime.UtcNow };
                    var infoDir = Path.Combine(_rootFolder, InfoFolderName);

                    if (!Directory.Exists(infoDir))
                    {
                        Directory.CreateDirectory(infoDir);
                    }

                    var infoFile = Path.Combine(infoDir, Path.GetFileName(avatarPath) + ".json");
                    File.WriteAllText(infoFile, JsonSerializer.Serialize(contentInfo, ContentInfoContext.Default.ContentInfo));
                }
                catch (Exception exc)
                {
                    _logger.LogWarning(exc, "Avatar use date update error: {error}", exc.Message);
                }

                return avatarPath;
            },
            cancellationToken);

    private string BuildAvatarPath(string avatarName, string avatarHashString) =>
        Path.Combine(
            _rootFolder,
            $"{avatarHashString.HashString()}_{avatarName.HashString()}" +
            $"{SecurityHelper.GetSafeExtension(avatarName)}");
}
