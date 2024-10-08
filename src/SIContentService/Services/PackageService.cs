﻿using Microsoft.Extensions.Options;
using SIContentService.Configuration;
using SIContentService.Contract.Models;
using SIContentService.Contracts;
using SIContentService.Exceptions;
using SIContentService.Helpers;
using SIContentService.Metrics;
using SIContentService.Models;
using System.IO.Compression;
using System.Net;
using System.Text.Json;
using ZipUtils;

namespace SIContentService.Services;

/// <inheritdoc cref="IPackageService" />
public sealed class PackageService : IPackageService
{
    private const string InfoFileName = "info.json";
    private const string QualityMarker = "quality.marker";
    private const int UnZipMaxFactor = 2;

    private readonly CollectionLocker _locker = new();

    private readonly IStorageService _storageService;
    private readonly SIContentServiceOptions _options;
    private readonly OtelMetrics _metrics;
    private readonly ILogger<PackageService> _logger;
    private readonly string _rootFolder;

    private readonly ExtractionOptions _extractionOptions;
    private readonly ExtractionOptions _qualityExtractionOptions;

    public PackageService(
        IStorageService storageService,
        IOptions<SIContentServiceOptions> options,
        OtelMetrics metrics,
        ILogger<PackageService> logger)
    {
        _storageService = storageService;
        _options = options.Value;
        _metrics = metrics;
        _logger = logger;

        _rootFolder = Path.Combine(StringHelper.BuildRootedPath(options.Value.ContentFolder), "packages");

        _extractionOptions = new ExtractionOptions(NamingModeSelector)
        {
            MaxAllowedDataLength = (long)_options.MaxPackageSizeMb * 1024 * 1024 * UnZipMaxFactor,
            FileFilter = FileFilter
        };

        _qualityExtractionOptions = new ExtractionOptions(NamingModeSelector)
        {
            MaxAllowedDataLength = (long)_options.MaxQualityPackageSizeMb * 1024 * 1024 * UnZipMaxFactor,
            FileFilter = FileFilter
        };
    }

    public async Task<(bool, string)> ImportUserPackageAsync(
        string filePath,
        string packageName,
        string packageHashString,
        CancellationToken cancellationToken = default)
    {
        var hasQualityControl = DetectPackageHasQualityControl(filePath);
        _storageService.ValidatePackageFile(filePath, hasQualityControl);
        
        var extractedFilePath = BuildPackagePath(packageName, packageHashString);

        if (PackagePathExists(extractedFilePath))
        {
            _logger.LogInformation("Folder {folder} already created. Package name: {packageName}", extractedFilePath, packageName);
            return (false, extractedFilePath);
        }

        await _locker.DoAsync(
            extractedFilePath,
            () => ExtractPackageAsync(filePath, extractedFilePath, hasQualityControl, cancellationToken),
            cancellationToken);

        _metrics.AddPackage();

        return (true, extractedFilePath);
    }

    public Task<string?> TryGetPackagePathAndUpdateUsageAsync(
        string packageName,
        string packageHashString,
        CancellationToken cancellationToken = default)
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
                                var contentInfo = JsonSerializer.Deserialize(info, ContentInfoContext.Default.ContentInfo);
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
                            _metrics.DeletePackage();
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

    private static bool PackagePathExists(string packagePath) =>
        Directory.Exists(packagePath) && Directory.EnumerateFiles(packagePath).Any(name => name != InfoFileName);

    private Task<string?> UpdatePackageUsageAsync(string packagePath, CancellationToken cancellationToken) =>
        _locker.DoWithLockAsync(
            packagePath,
            () =>
            {
                if (!PackagePathExists(packagePath))
                {
                    return null;
                }

                try
                {
                    var contentInfo = new ContentInfo { LastUsageTime = DateTime.UtcNow };

                    if (Directory.Exists(packagePath))
                    {
                        var infoFile = Path.Combine(packagePath, InfoFileName);
                        File.WriteAllText(infoFile, JsonSerializer.Serialize(contentInfo, ContentInfoContext.Default.ContentInfo));
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
        bool hasQualityControl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var extractedFiles = await ZipExtractor.ExtractArchiveFileToFolderAsync(
                filePath,
                extractedFilePath,
                hasQualityControl ? _qualityExtractionOptions : _extractionOptions,
                cancellationToken);

            File.WriteAllLines(Path.Combine(extractedFilePath, "filesMap.txt"), extractedFiles.Select(f => $"{f.Key}:{f.Value.Name}:{f.Value.Size}"));
        }
        catch (InvalidDataException exc)
        {
            throw new ServiceException(
                WellKnownSIContentServiceErrorCode.BadPackageFile,
                HttpStatusCode.BadRequest,
                exc);
        }
        catch (ServiceException exc) when (exc.ErrorCode == WellKnownSIContentServiceErrorCode.FileTooLarge)
        {
            DeleteFolder(extractedFilePath);
            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            DeleteFolder(extractedFilePath);
            throw;
        }
        catch (Exception exc)
        {
            _logger.LogWarning(exc, "ExtractPackageAsync error: {error}", exc.Message);
            DeleteFolder(extractedFilePath);

            throw new ServiceException(
                WellKnownSIContentServiceErrorCode.BadPackageFile,
                HttpStatusCode.BadRequest,
                exc);
        }
    }

    private bool DetectPackageHasQualityControl(string filePath)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
            return archive.Entries.Any(entry => entry.FullName == QualityMarker);
        }
        catch (Exception exc)
        {
            _logger.LogWarning(exc, "DetectHasQualityControl error: {error}", exc.Message);
            return false;
        }
    }

    private void DeleteFolder(string extractedFilePath)
    {
        try
        {
            Directory.Delete(extractedFilePath, true);
        }
        catch (Exception exc)
        {
            _logger.LogWarning(
                exc,
                "ExtractPackageAsync clear directory {directory} error: {error}",
                extractedFilePath,
                exc.Message);
        }
    }

    private static UnzipNamingMode NamingModeSelector(string name) => name switch
    {
        "content.xml" or QualityMarker or "Texts/authors.xml" or "Texts/sources.xml" => UnzipNamingMode.KeepOriginal,
        _ => UnzipNamingMode.Hash // This guarantees that we never use user-provided file names
    };

    private static bool FileFilter(string filePath)
    {
        if (filePath == "content.xml" || filePath == QualityMarker)
        {
            return true;
        }

        var folderName = Path.GetDirectoryName(filePath);

        return folderName switch
        {
            "Images" or "Audio" or "Video" or "Html" or "Texts" => true,
            _ => false
        };
    }
}
