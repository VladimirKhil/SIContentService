using EnsureThat;
using Microsoft.Extensions.Options;
using SIContentService.Configuration;
using SIContentService.Contract.Models;
using SIContentService.Contracts;
using SIContentService.Exceptions;
using SIContentService.Helpers;
using System.Net;

namespace SIContentService.Services;

/// <inheritdoc cref="IStorageService" />
internal sealed class StorageService : IStorageService
{
    private readonly string _storageRoot;
    private readonly SIContentServiceOptions _options;

    public StorageService(IOptions<SIContentServiceOptions> options)
    {
        _options = options.Value;
        var root = Path.GetPathRoot(StringHelper.BuildRootedPath(options.Value.ContentFolder));

        Ensure.That(root).IsNotNull();

        _storageRoot = root!;
    }

    public bool CheckFreeSpace()
    {
        var freeSpace = new DriveInfo(_storageRoot).TotalFreeSpace;
        var hasFreeSpace = freeSpace >= (long)_options.MinDriveFreeSpaceMb * 1024 * 1024;

        return hasFreeSpace;
    }

    public bool IsFreeSpaceCritical()
    {
        var freeSpace = new DriveInfo(_storageRoot).TotalFreeSpace;
        return freeSpace < ((long)_options.MinDriveFreeSpaceMb + (long)_options.MinDriveCriticalSpaceMb) * 1024 * 1024;
    }

    public void ValidatePackageFile(string filePath)
    {
        var maxPackageSizeFactor = IsFreeSpaceCritical() ? 0.5 : 1;
        var maxPackageSize = _options.MaxPackageSizeMb * 1024 * 1024 * maxPackageSizeFactor;

        var fileLength = new FileInfo(filePath).Length;

        if (fileLength == 0)
        {
            throw new ServiceException(WellKnownSIContentServiceErrorCode.FileEmpty, HttpStatusCode.BadRequest);
        }

        if (fileLength > maxPackageSize)
        {
            throw new ServiceException(
                WellKnownSIContentServiceErrorCode.FileTooLarge,
                HttpStatusCode.RequestEntityTooLarge,
                new Dictionary<string, object>
                {
                    ["maxSizeMb"] = maxPackageSize
                });
        }
    }
}
