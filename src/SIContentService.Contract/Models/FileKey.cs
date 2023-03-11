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

    public FileKey()
    {
        Name = "";
        Hash = Array.Empty<byte>();
    }

    public FileKey(string name, byte[] hash)
    {
        Name = name;
        Hash = hash;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not FileKey other)
        {
            return base.Equals(obj);
        }

        return Name == other.Name && HashString == other.HashString;
    }

    public override int GetHashCode() => HashCode.Combine(HashString, Name);

    public override string ToString() => $"{HashString}_{Name}";

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
