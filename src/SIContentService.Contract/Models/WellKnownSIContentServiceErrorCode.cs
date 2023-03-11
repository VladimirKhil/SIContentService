namespace SIContentService.Contract.Models;

/// <summary>
/// Defines well-known SIContent service error codes.
/// </summary>
public enum WellKnownSIContentServiceErrorCode
{
    /// <summary>
    /// Bad package file.
    /// </summary>
    BadPackageFile,

    /// <summary>
    /// Storage is full.
    /// </summary>
    StorageFull,

    /// <summary>
    /// File is empty.
    /// </summary>
    FileEmpty,

    /// <summary>
    /// File is too large.
    /// </summary>
    FileTooLarge,

    /// <summary>
    /// Multipart ContentType is required.
    /// </summary>
    MultipartContentTypeRequired,

    /// <summary>
    /// Content-Length header is required.
    /// </summary>
    ContentLengthHeaderRequired,

    /// <summary>
    /// Content-Disposition header is required.
    /// </summary>
    ContentDispositionHeaderRequired,

    /// <summary>
    /// Disposition Name is required.
    /// </summary>
    DispositionNameRequired,

    /// <summary>
    /// Disposition FileName is required.
    /// </summary>
    DispositionFileNameRequired,

    /// <summary>
    /// ContentMD5 header is required.
    /// </summary>
    ContentMD5HeaderRequired,
}
