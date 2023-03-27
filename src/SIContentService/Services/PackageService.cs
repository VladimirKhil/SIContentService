using Microsoft.Extensions.Options;
using SIContentService.Configuration;
using SIContentService.Contract.Helpers;
using SIContentService.Contract.Models;
using SIContentService.Contracts;
using SIContentService.Exceptions;
using SIContentService.Helpers;
using SIContentService.Models;
using SIPackages.Helpers;
using System.Net;
using System.Text.Json;

namespace SIContentService.Services;

/// <inheritdoc cref="IPackageService" />
public sealed class PackageService : IPackageService
{
    private const string InfoFileName = "info.json";
    internal const int UnZipMaxFactor = 2;

    private readonly CollectionLocker _locker = new();

    private readonly IStorageService _storageService;
    private readonly SIContentServiceOptions _options;
    private readonly ILogger<PackageService> _logger;
    private readonly string _rootFolder;

    public PackageService(
        IStorageService storageService,
        IOptions<SIContentServiceOptions> options, 
        ILogger<PackageService> logger)
    {
        _storageService = storageService;
        _options = options.Value;
        _logger = logger;

        _rootFolder = Path.Combine(_options.ContentFolder, "packages");
    }

    public async Task<string> ImportUserPackageAsync(
        string filePath,
        string packageName,
        string packageHashString,
        CancellationToken cancellationToken = default)
    {
        if (!_storageService.CheckFreeSpace())
        {
            throw new ServiceException(WellKnownSIContentServiceErrorCode.StorageFull, HttpStatusCode.InsufficientStorage);
        }

        var extractedFilePath = BuildPackagePath(packageName, packageHashString);

        if (Directory.Exists(extractedFilePath) && Directory.EnumerateFiles(extractedFilePath).Count() > 1)
        {
            _logger.LogWarning("Folder {folder} already created! Package name: {packageName}", extractedFilePath, packageName);
        }
        else
        {
            await _locker.DoAsync(
                extractedFilePath,
                () => ExtractPackageAsync(filePath, extractedFilePath, cancellationToken),
                cancellationToken);
        }

        return extractedFilePath;
    }

    public Task<string?> TryGetPackagePathAsync(string packageName, string packageHashString, CancellationToken cancellationToken = default)
    {
        var packagePath = BuildPackagePath(packageName, packageHashString);
        return UpdatePackageUsageAsync(packagePath, cancellationToken);
    }

    public async Task ClearOldPackagesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var now = DateTimeOffset.UtcNow;

            var dirInfo = new DirectoryInfo(_rootFolder);

            if (!dirInfo.Exists)
            {
                return;
            }

            foreach (var dir in dirInfo.EnumerateDirectories())
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                await _locker.DoWithLockAsync(
                    dir.FullName,
                    () =>
                    {
                        var infoFile = Path.Combine(dir.FullName, InfoFileName);

                        DateTimeOffset lastUsageTime;

                        if (File.Exists(infoFile))
                        {
                            try
                            {
                                var info = File.ReadAllText(infoFile);
                                var contentInfo = JsonSerializer.Deserialize<ContentInfo>(info);
                                lastUsageTime = contentInfo?.LastUsageTime ?? dir.CreationTime;
                            }
                            catch (Exception exc)
                            {
                                _logger.LogWarning(exc, "Opening info file error: {error}", exc.Message);
                                lastUsageTime = dir.CreationTime;
                            }
                        }
                        else
                        {
                            lastUsageTime = dir.CreationTime;
                        }

                        if (now.Subtract(lastUsageTime) < _options.MaxPackageLifetime)
                        {
                            return;
                        }

                        try
                        {
                            dir.Delete(true);
                        }
                        catch (Exception exc)
                        {
                            _logger.LogError(exc, "Error deleting folder {folderName}: {error}", dir.FullName, exc.Message);
                        }
                    },
                    cancellationToken);
            }
        }
        catch (Exception exc)
        {
            _logger.LogError(exc, "Clear old packages error: {error}", exc.Message);
        }
    }

    private string BuildPackagePath(string packageName, string packageHashString) =>
        Path.Combine(_rootFolder, $"{packageHashString.HashString()}_{packageName.HashString()}");

    private Task<string?> UpdatePackageUsageAsync(string packagePath, CancellationToken cancellationToken) =>
        _locker.DoWithLockAsync(
            packagePath,
            () =>
            {
                if (!Directory.Exists(packagePath) || Directory.EnumerateFiles(packagePath).All(name => name == InfoFileName))
                {
                    return null;
                }

                try
                {
                    var contentInfo = new ContentInfo { LastUsageTime = DateTime.UtcNow };

                    if (Directory.Exists(packagePath))
                    {
                        var infoFile = Path.Combine(packagePath, InfoFileName);
                        File.WriteAllText(infoFile, JsonSerializer.Serialize(contentInfo));
                    }
                }
                catch (Exception exc)
                {
                    _logger.LogWarning(exc, "Package use date update error: {error}", exc.Message);
                }

                return packagePath;
            },
            cancellationToken);

    private async Task ExtractPackageAsync(
        string filePath,
        string extractedFilePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO: rewrite without SIPackages
            var filteredFiles = await ZipHelper.ExtractToDirectoryAsync(
                filePath,
                extractedFilePath,
                ExtractedFileNamingModes.Hash, // This guarantees that we never use user-provided file names
                (long)(_options.MaxPackageSizeMb) * 1024 * 1024 * UnZipMaxFactor,
                null,
                cancellationToken);

            if (filteredFiles.Length > 0)
            {
                File.WriteAllLines(Path.Combine(extractedFilePath, "filtered.txt"), filteredFiles);
            }
        }
        catch (InvalidDataException exc)
        {
            throw new ServiceException(
                WellKnownSIContentServiceErrorCode.BadPackageFile,
                HttpStatusCode.BadRequest,
                exc);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exc)
        {
            _logger.LogWarning(exc, "ExtractPackageAsync error: {error}", exc.Message);

            try
            {
                Directory.Delete(extractedFilePath, true);
            }
            catch (Exception exc2)
            {
                _logger.LogWarning(
                    exc,
                    "ExtractPackageAsync clear directory {directory} error: {error}",
                    extractedFilePath,
                    exc2.Message);
            }

            throw new ServiceException(
                WellKnownSIContentServiceErrorCode.BadPackageFile,
                HttpStatusCode.BadRequest,
                exc);
        }
    }
}
