namespace SIContentService.Contract.Models;

/// <summary>
/// Defines a unique file key.
/// </summary>
public sealed class FileKey
{
    /// <summary>
    /// File name.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// File hash.
    /// </summary>
    public byte[] Hash { get; init; }

    private string HashString => Convert.ToBase64String(Hash);

    /// <summary>
    /// Initializes a new instane of <see cref="FileKey" /> class.
    /// </summary>
    public FileKey()
    {
        Name = "";
        Hash = Array.Empty<byte>();
    }

    /// <summary>
    /// Initializes a new instane of <see cref="FileKey" /> class.
    /// </summary>
    /// <param name="name">File name.</param>
    /// <param name="hash">File hash.</param>
    public FileKey(string name, byte[] hash)
    {
        Name = name;
        Hash = hash;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is not FileKey other)
        {
            return base.Equals(obj);
        }

        return Name == other.Name && HashString == other.HashString;
    }

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(HashString, Name);

    /// <inheritdoc />
    public override string ToString() => $"{HashString}_{Name}";

    /// <summary>
    /// Parses a string into a <see cref="FileKey" />.
    /// </summary>
    /// <param name="s">Input string.</param>
    /// <returns>Parsed value.</returns>
    /// <exception cref="InvalidCastException">Input string was in an incorrect format.</exception>
    public static FileKey Parse(string s)
    {
        var index = s.IndexOf('_');

        if (index == -1)
        {
            throw new InvalidCastException($"Could not cast value {s} to FileKey");
        }

        return new FileKey(s[(index + 1)..], Convert.FromBase64String(s[..index]));
    }
}
