using Microsoft.AspNetCore.Mvc;
using SIContentService.Contract.Models;
using SIContentService.Contracts;
using SIContentService.Exceptions;
using System.Net;
using System.Text;

namespace SIContentService.Controllers;

/// <summary>
/// Provides API for importing content from external source.
/// </summary>
[Route("api/v1/import")]
[ApiController]
public sealed class ImportController : ControllerBase
{
    private readonly IPackageService _packageService;
    private readonly IStorageService _storageService;
    private readonly IResourceDownloader _resourceDownloader;
    private readonly ILogger<ImportController> _logger;

    public ImportController(
        IPackageService packageService,
        IStorageService storageService,
        IResourceDownloader resourceDownloader,
        ILogger<ImportController> logger)
    {
        _packageService = packageService;
        _storageService = storageService;
        _resourceDownloader = resourceDownloader;
        _logger = logger;
    }

    [HttpPut("packages")]
    public async Task<IActionResult> ImportPackage(
        [FromBody] ImportRequest importRequest,
        CancellationToken cancellationToken = default)
    {
        var packageName = "";
        var escapedHash = importRequest.SourceUri.AbsoluteUri;

        var packagePath = await _packageService.TryGetPackagePathAndUpdateUsageAsync(packageName, escapedHash, cancellationToken);

        if (packagePath != null)
        {
            return Ok($"/packages/{Path.GetFileName(packagePath)}");
        }

        if (!_storageService.CheckFreeSpace())
        {
            throw new ServiceException(WellKnownSIContentServiceErrorCode.StorageFull, HttpStatusCode.InsufficientStorage);
        }

        var targetFilePath = Path.GetTempFileName();

        try
        {
            await _resourceDownloader.DownloadResourceAsync(importRequest.SourceUri, targetFilePath, cancellationToken);
            _storageService.ValidatePackageFile(targetFilePath);

            var (success, filePath) = await _packageService.ImportUserPackageAsync(
                targetFilePath,
                packageName,
                escapedHash,
                cancellationToken);

            if (!success)
            {
                var packageData = $"{escapedHash} {packageName}";
                Response.Headers.Add("Not-Created", Convert.ToBase64String(Encoding.UTF8.GetBytes(packageData)));
            }

            return Ok($"/packages/{Path.GetFileName(filePath)}");
        }
        finally
        {
            if (System.IO.File.Exists(targetFilePath))
            {
                try
                {
                    System.IO.File.Delete(targetFilePath);
                }
                catch (Exception exc)
                {
                    _logger.LogWarning(exc, "File delete error: {error}", exc.Message);
                }
            }
        }
    }
}
