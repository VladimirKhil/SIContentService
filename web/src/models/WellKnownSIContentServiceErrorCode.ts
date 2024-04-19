/** Defines well-known SIContent service error codes. */
const enum WellKnownSIContentServiceErrorCode {
    /** Bad package file. */
    BadPackageFile,

    /** Storage is full. */
    StorageFull,

    /** File is empty. */
    FileEmpty,

    /** File is too large. */
    FileTooLarge,

    /** Multipart ContentType is required. */
    MultipartContentTypeRequired,

    /** Content-Length header is required. */
    ContentLengthHeaderRequired,

    /** Content-Disposition header is required. */
    ContentDispositionHeaderRequired,

    /** Disposition Name is required. */
    DispositionNameRequired,

    /** Disposition FileName is required. */
    DispositionFileNameRequired,

    /** ContentMD5 header is required. */
    ContentMD5HeaderRequired,
}

export default WellKnownSIContentServiceErrorCode;
