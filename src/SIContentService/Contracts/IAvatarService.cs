namespace SIContentService.Contracts;

/// <summary>
/// Provides API for working woth avatars.
/// </summary>
public interface IAvatarService
{
    /// <summary>
    /// Adds new avatar.
    /// </summary>
    /// <param name="avatarName">Avatar name.</param>
    /// <param name="avatarHashString">Avatar hash.</param>
    /// <param name="fileWriteAsync">Method for writing avatar contents.</param>
    Task<string> AddAvatarAsync(string avatarName, string avatarHashString, Func<Stream, Task> fileWriteAsync);

    /// <summary>
    /// Gets avatar path.
    /// </summary>
    /// <param name="avatarName">Avatar name.</param>
    /// <param name="avatarHashString">Avatar hash.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<string?> TryGetAvatarPathAsync(string avatarName, string avatarHashString, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes old avatars.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ClearOldAvatarsAsync(CancellationToken cancellationToken);
}
