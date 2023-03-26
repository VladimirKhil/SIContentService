﻿using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using SIContentService.Attributes;
using SIContentService.Configuration;
using SIContentService.Contract.Models;
using SIContentService.Contracts;
using SIContentService.Exceptions;
using SIContentService.Helpers;
using System.Net;

namespace SIContentService.Controllers;

/// <summary>
/// Provides API for uploading and requesting package and avatar content.
/// </summary>
[Route("api/v1/content")]
[ApiController]
public sealed class ContentController : ControllerBase
{
    private static readonly FormOptions DefaultFormOptions = new();
    private const long MaxPackageSizeBytes = 105_000_000;
    private const long MaxAvatarSizeBytes = 2_000_000;

    private readonly IPackageService _packageService;
    private readonly IAvatarService _avatarService;
    private readonly IStorageService _storageService;
    private readonly SIContentServiceOptions _options;
    private readonly ILogger<ContentController> _logger;

    public ContentController(
        IPackageService packageService,
        IAvatarService avatarService,
        IStorageService storageService,
        IOptions<SIContentServiceOptions> options,
        ILogger<ContentController> logger)
    {
        _packageService = packageService;
        _avatarService = avatarService;
        _storageService = storageService;
        _options = options.Value;
        _logger = logger;
    }

    [HttpPost("packages")]
    [DisableFormValueModelBinding]
    [RequestSizeLimit(MaxPackageSizeBytes)]
    public async Task<IActionResult> UploadPackage()
    {
        var cancellationToken = HttpContext.RequestAborted; // Binding is disabled so don't inject cancellation token as method parameter

        try
        {
            if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                throw new ServiceException(WellKnownSIContentServiceErrorCode.MultipartContentTypeRequired, HttpStatusCode.BadRequest);
            }

            if (!Request.ContentLength.HasValue)
            {
                throw new ServiceException(WellKnownSIContentServiceErrorCode.ContentLengthHeaderRequired, HttpStatusCode.BadRequest);
            }

            if (!_storageService.CheckFreeSpace())
            {
                throw new ServiceException(WellKnownSIContentServiceErrorCode.StorageFull, HttpStatusCode.InsufficientStorage);
            }

            var boundary = MultipartRequestHelper.GetBoundary(
                MediaTypeHeaderValue.Parse(Request.ContentType),
                DefaultFormOptions.MultipartBoundaryLengthLimit);

            var reader = new MultipartReader(boundary, HttpContext.Request.Body);

            var section = await reader.ReadNextSectionAsync(cancellationToken);

            string? packagePath = null;

            while (section != null)
            {
                var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition);

                if (hasContentDispositionHeader)
                {
                    if (MultipartRequestHelper.HasFileContentDisposition(contentDisposition!))
                    {
                        packagePath = await ReadUploadedFileAsync(section.Body, contentDisposition!, cancellationToken);
                    }
                    else
                    {
                        throw new ServiceException(WellKnownSIContentServiceErrorCode.ContentDispositionHeaderRequired, HttpStatusCode.BadRequest);
                    }
                }

                // Drains any remaining section body that has not been consumed and
                // reads the headers for the next section
                section = await reader.ReadNextSectionAsync(cancellationToken);
            }

            if (packagePath == null)
            {
                throw new ServiceException(WellKnownSIContentServiceErrorCode.FileEmpty, HttpStatusCode.BadRequest);
            }

            return Ok($"/packages/{Path.GetFileName(packagePath)}");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // No action
            return Accepted();
        }
    }

    [HttpGet("packages/{packageHash}/{packageName}")]
    public async Task<ActionResult<string?>> GetPackageUriAsync(
        string packageHash,
        string packageName,
        CancellationToken cancellationToken = default)
    {
        var decodedHash = Uri.UnescapeDataString(packageHash);
        var decodedName = Uri.UnescapeDataString(packageName);

        var packagePath = await _packageService.TryGetPackagePathAsync(decodedName, decodedHash, cancellationToken);
        return packagePath != null ? Ok($"/packages/{Path.GetFileName(packagePath)}") : NotFound();
    }

    [HttpPost("avatars")]
    [RequestSizeLimit(MaxAvatarSizeBytes)]
    public async Task<IActionResult> UploadAvatar(CancellationToken cancellationToken = default)
    {
        var files = Request.Form.Files;

        if (files.Count == 0)
        {
            throw new ServiceException(WellKnownSIContentServiceErrorCode.FileEmpty, HttpStatusCode.BadRequest);
        }

        var file = files[0];

        if (file == null || file.Length == 0)
        {
            throw new ServiceException(WellKnownSIContentServiceErrorCode.FileEmpty, HttpStatusCode.BadRequest);
        }

        if (!_storageService.CheckFreeSpace())
        {
            throw new ServiceException(WellKnownSIContentServiceErrorCode.StorageFull, HttpStatusCode.InsufficientStorage);
        }

        string avatarName = StringHelper.UnquoteValue(file.FileName);

        if (file.Length > _options.MaxAvatarSizeMb * 1024 * 1024)
        {
            throw new ServiceException(
                WellKnownSIContentServiceErrorCode.FileTooLarge,
                HttpStatusCode.RequestEntityTooLarge,
                new Dictionary<string, object>
                {
                    ["maxSizeMb"] = _options.MaxAvatarSizeMb
                });
        }

        var md5Headers = Request.Headers.ContentMD5;

        var avatarHashString = (md5Headers.Count > 0 ? md5Headers[0] : file.Name)
            ?? throw new ServiceException(WellKnownSIContentServiceErrorCode.ContentMD5HeaderRequired, HttpStatusCode.BadRequest);
        
        var avatarPath = await _avatarService.AddAvatarAsync(
            avatarName,
            avatarHashString,
            stream => file.CopyToAsync(stream, cancellationToken));

        return Ok($"/avatars/{Path.GetFileName(avatarPath)}");
    }

    [HttpGet("avatars/{avatarHash}/{avatarName}")]
    public async Task<ActionResult<string?>> GetAvatarUriAsync(string avatarHash, string avatarName, CancellationToken cancellationToken = default)
    {
        var decodedHash = Uri.UnescapeDataString(avatarHash);
        var decodedName = Uri.UnescapeDataString(avatarName);

        var avatarPath = await _avatarService.TryGetAvatarPathAsync(decodedName, decodedHash, cancellationToken);
        return avatarPath != null ? Ok($"/avatars/{Path.GetFileName(avatarPath)}") : NotFound();
    }

    private async Task<string> ReadUploadedFileAsync(
        Stream fileStream,
        ContentDispositionHeaderValue contentDisposition,
        CancellationToken cancellationToken)
    {
        var targetFilePath = Path.GetTempFileName();

        try
        {
            using (var targetStream = System.IO.File.Create(targetFilePath))
            {
                await fileStream.CopyToAsync(targetStream, cancellationToken);
            }

            var maxPackageSizeFactor = _storageService.IsFreeSpaceCritical() ? 0.5 : 1;
            var maxPackageSize = _options.MaxPackageSizeMb * 1024 * 1024 * maxPackageSizeFactor;

            var fileLength = new FileInfo(targetFilePath).Length;

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

            var md5Headers = Request.Headers.ContentMD5;
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

            return await _packageService.ImportUserPackageAsync(targetFilePath, packageName, packageHashString, cancellationToken);
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
