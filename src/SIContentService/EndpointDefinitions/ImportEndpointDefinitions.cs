using Microsoft.AspNetCore.Mvc;
using SIContentService.Contract.Models;
using SIContentService.Contracts;
using SIContentService.Exceptions;
using System.Net;
using System.Text;

namespace SIContentService.EndpointDefinitions;

/// <summary>
/// Provides API for importing content from external source.
/// </summary>
internal static class ImportEndpointDefinitions
{
    public static void DefineContentEndpoint(WebApplication app)
    {
        app.MapPut("api/v1/import/packages", async (
            HttpContext context,
            [FromBody] ImportRequest importRequest,
            IPackageService packageService,
            IStorageService storageService,
            IResourceDownloader resourceDownloader,
            CancellationToken cancellationToken) =>
        {
            var packageName = "";
            var escapedHash = importRequest.SourceUri.AbsoluteUri;

            var packagePath = await packageService.TryGetPackagePathAndUpdateUsageAsync(packageName, escapedHash, cancellationToken);

            if (packagePath != null)
            {
                return Results.Text($"/packages/{Path.GetFileName(packagePath)}");
            }

            if (!storageService.CheckFreeSpace())
            {
                throw new ServiceException(WellKnownSIContentServiceErrorCode.StorageFull, HttpStatusCode.InsufficientStorage);
            }

            var targetFilePath = Path.GetTempFileName();

            try
            {
                await resourceDownloader.DownloadResourceAsync(importRequest.SourceUri, targetFilePath, cancellationToken);
                storageService.ValidatePackageFile(targetFilePath);

                var (success, filePath) = await packageService.ImportUserPackageAsync(
                    targetFilePath,
                    packageName,
                    escapedHash,
                    cancellationToken);

                if (!success)
                {
                    var packageData = $"{escapedHash} {packageName}";
                    context.Response.Headers.Append("Not-Created", Convert.ToBase64String(Encoding.UTF8.GetBytes(packageData)));
                }

                return Results.Text($"/packages/{Path.GetFileName(filePath)}");
            }
            finally
            {
                if (File.Exists(targetFilePath))
                {
                    try
                    {
                        File.Delete(targetFilePath);
                    }
                    catch (Exception exc)
                    {
                        app.Logger.LogWarning(exc, "File delete error: {error}", exc.Message);
                    }
                }
            }
        });
    }
}
