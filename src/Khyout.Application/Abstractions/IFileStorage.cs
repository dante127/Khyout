namespace Khyout.Application.Abstractions;

/// <summary>Stores media bytes by content-derived file name.</summary>
public interface IFileStorage
{
    /// <summary>Writes the bytes and returns the relative storage path (file name).</summary>
    Task<string> SaveAsync(string fileName, byte[] bytes, CancellationToken cancellationToken = default);
}
