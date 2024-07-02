using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using SIContentService.Attributes;
using SIContentService.Configuration;
using SIContentService.Contract.Helpers;
using SIContentService.Contract.Models;
using SIContentService.Contracts;
using SIContentService.Exceptions;
using SIContentService.Helpers;
using System.Net;
using System.Text;

namespace SIContentService.EndpointDefinitions;

/// <summary>
/// Provides API for uploading and requesting package and avatar content.
/// </summary>
internal static class ContentEndpointDefinitions
{
    private static readonly FormOptions DefaultFormOptions = new();

    public static void DefineContentEndpoint(WebApplication app, SIContentServiceOptions options)
    {
        app.MapPost("/api/v1/content/packages", [DisableFormValueModelBinding] async (
            HttpContext context,
            IPackageService packageService,
            IStorageService storageService,
            IOptions<SIContentServiceOptions> options) =>
        {
            var cancellationToken = context.RequestAborted; // Binding is disabled so don't inject cancellation token as method parameter
            var request = context.Request;

            try
            {
                if (!MultipartRequestHelper.IsMultipartContentType(request.ContentType))
                {
                    throw new ServiceException(WellKnownSIContentServiceErrorCode.MultipartContentTypeRequired, HttpStatusCode.BadRequest);
                }

                if (!request.ContentLength.HasValue)
                {
                    throw new ServiceException(WellKnownSIContentServiceErrorCode.ContentLengthHeaderRequired, HttpStatusCode.BadRequest);
                }

                if (!storageService.CheckFreeSpace())
                {
                    throw new ServiceException(WellKnownSIContentServiceErrorCode.StorageFull, HttpStatusCode.InsufficientStorage);
                }

                var boundary = MultipartRequestHelper.GetBoundary(
                    MediaTypeHeaderValue.Parse(request.ContentType),
                    DefaultFormOptions.MultipartBoundaryLengthLimit);

                var reader = new MultipartReader(boundary, request.Body);

                var section = await reader.ReadNextSectionAsync(cancellationToken);

                string? packagePath = null;

                while (section != null)
                {
                    var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition);

                    if (hasContentDispositionHeader && contentDisposition != null)
                    {
                        if (MultipartRequestHelper.HasFileContentDisposition(contentDisposition))
                        {
                            packagePath = await ReadUploadedFileAsync(
                                section.Body,
                                context,
                                contentDisposition,
                                packageService,
                                storageService,
                                options.Value,
                                app.Logger,
                                cancellationToken);
                        }
                        else
                        {
                            throw new ServiceException(
                                WellKnownSIContentServiceErrorCode.ContentDispositionHeaderRequired,
                                HttpStatusCode.BadRequest);
                        }
                    }

                    section = await reader.ReadNextSectionAsync(cancellationToken);
                }

                if (packagePath == null)
                {
                    throw new ServiceException(WellKnownSIContentServiceErrorCode.FileEmpty, HttpStatusCode.BadRequest);
                }

                return Results.Text($"/packages/{Path.GetFileName(packagePath)}");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return Results.Accepted();
            }
        }).WithMetadata(new RequestSizeLimitAttribute((options.MaxPackageSizeMb + 1) * 1024 * 1024));

        app.MapGet("/api/v1/content/packages/{packageHash}/{packageName}", async (
            string packageHash,
            string packageName,
            IPackageService packageService,
            CancellationToken cancellationToken) =>
        {
            var decodedName = Uri.UnescapeDataString(packageName);

            var packagePath = await packageService.TryGetPackagePathAndUpdateUsageAsync(decodedName, packageHash, cancellationToken);
            return packagePath != null ? Results.Text($"/packages/{Path.GetFileName(packagePath)}") : Results.NotFound();
        });

        app.MapPost("/api/v1/content/avatars", async (
            HttpContext context,
            IAvatarService avatarService,
            IStorageService storageService,
            IOptions<SIContentServiceOptions> options,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var files = context.Request.Form.Files;

                if (files.Count == 0)
                {
                    throw new ServiceException(WellKnownSIContentServiceErrorCode.FileEmpty, HttpStatusCode.BadRequest);
                }

                var file = files[0];

                if (file == null || file.Length == 0)
                {
                    throw new ServiceException(WellKnownSIContentServiceErrorCode.FileEmpty, HttpStatusCode.BadRequest);
                }

                if (!storageService.CheckFreeSpace())
                {
                    throw new ServiceException(WellKnownSIContentServiceErrorCode.StorageFull, HttpStatusCode.InsufficientStorage);
                }

                var avatarName = StringHelper.UnquoteValue(file.FileName);

                if (file.Length > options.Value.MaxAvatarSizeMb * 1024 * 1024)
                {
                    throw new ServiceException(
                        WellKnownSIContentServiceErrorCode.FileTooLarge,
                        HttpStatusCode.RequestEntityTooLarge,
                        new Dictionary<string, object>
                        {
                            ["maxSizeMb"] = options.Value.MaxAvatarSizeMb
                        });
                }

                var md5Headers = context.Request.Headers[HeaderNames.ContentMD5];

                var avatarHashString = (md5Headers.Count > 0 ? md5Headers[0] : file.Name)
                    ?? throw new ServiceException(WellKnownSIContentServiceErrorCode.ContentMD5HeaderRequired, HttpStatusCode.BadRequest);

                var avatarPath = await avatarService.AddAvatarAsync(
                    avatarName,
                    Base64Helper.EscapeBase64(avatarHashString),
                    stream => file.CopyToAsync(stream, cancellationToken));

                return Results.Text($"/avatars/{Path.GetFileName(avatarPath)}");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return Results.Accepted();
            }
        }).WithMetadata(new RequestSizeLimitAttribute((options.MaxAvatarSizeMb + 1) * 1024 * 1024));

        app.MapGet("/api/v1/content/avatars/{avatarHash}/{avatarName}", async (
            string avatarHash,
            string avatarName,
            IAvatarService avatarService,
            CancellationToken cancellationToken) =>
        {
            app.Logger.LogInformation("Avatar uri requested. Name: {name}, hash: {hash}", avatarName, avatarHash);

            var decodedName = Uri.UnescapeDataString(avatarName);

            var avatarPath = await avatarService.TryGetAvatarPathAsync(decodedName, avatarHash, cancellationToken);
            return avatarPath != null ? Results.Text($"/avatars/{Path.GetFileName(avatarPath)}") : Results.NotFound();
        });
    }

    private static async Task<string> ReadUploadedFileAsync(
        Stream fileStream,
        HttpContext context,
        ContentDispositionHeaderValue contentDisposition,
        IPackageService packageService,
        IStorageService storageService,
        SIContentServiceOptions options,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var (packageName, escapedHash) = ReadAndValidatePackageMetadata(context.Request, contentDisposition);

        var packagePath = await packageService.TryGetPackagePathAndUpdateUsageAsync(packageName, escapedHash, cancellationToken);

        if (packagePath != null)
        {
            return packagePath;
        }

        var targetFilePath = Path.GetTempFileName();

        try
        {
            using (var targetStream = File.Create(targetFilePath, options.BufferSize, FileOptions.Asynchronous))
            {
                await fileStream.CopyToAsync(targetStream, options.BufferSize, cancellationToken);
            }

            storageService.ValidatePackageFile(targetFilePath);

            var (success, filePath) = await packageService.ImportUserPackageAsync(
                targetFilePath,
                packageName,
                escapedHash,
                cancellationToken);

            if (!success)
            {
                var packageData = $"{escapedHash} {packageName}";
                context.Response.Headers.Add("Not-Created", Convert.ToBase64String(Encoding.UTF8.GetBytes(packageData)));
            }

            return filePath;
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
                    logger.LogWarning(exc, "File delete error: {error}", exc.Message);
                }
            }
        }
    }

    private static (string packageName, string escapedHash) ReadAndValidatePackageMetadata(
        HttpRequest request,
        ContentDispositionHeaderValue contentDisposition)
    {
        var md5Headers = request.Headers.ContentMD5;
        var fileNameValue = contentDisposition.Name.Value;
        var packageNameValue = contentDisposition.FileName.Value;

        if (fileNameValue == null)
        {
            throw new ServiceException(WellKnownSIContentServiceErrorCode.DispositionNameRequired, HttpStatusCode.BadRequest);
        }

        if (packageNameValue == null)
        {
            throw new ServiceException(WellKnownSIContentServiceErrorCode.DispositionFileNameRequired, HttpStatusCode.BadRequest);
        }

        var fileName = StringHelper.UnquoteValue(fileNameValue);
        var packageName = StringHelper.UnquoteValue(packageNameValue);

        var packageHashString = (md5Headers.Count > 0 ? md5Headers[0] : fileName)
            ?? throw new ServiceException(WellKnownSIContentServiceErrorCode.ContentMD5HeaderRequired, HttpStatusCode.BadRequest);

        var escapedHash = Base64Helper.EscapeBase64(packageHashString);

        return (packageName, escapedHash);
    }
}
