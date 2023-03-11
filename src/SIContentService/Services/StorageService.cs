using EnsureThat;
using Microsoft.Extensions.Options;
using SIContentService.Configuration;
using SIContentService.Contracts;

namespace SIContentService.Services;

/// <inheritdoc cref="IStorageService" />
internal sealed class StorageService : IStorageService
{
    private readonly string _storageRoot;
    private readonly SIContentServiceOptions _options;

    public StorageService(IOptions<SIContentServiceOptions> options)
    {
        _options = options.Value;
        var root = Path.GetPathRoot(_options.ContentFolder);

        if (string.IsNullOrEmpty(root))
        {
            root = Path.GetPathRoot(Environment.CurrentDirectory);
        }

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
}
