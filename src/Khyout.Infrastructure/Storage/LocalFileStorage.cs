using Khyout.Application.Abstractions;

namespace Khyout.Infrastructure.Storage;

/// <summary>Local-disk media storage rooted at the configured directory.</summary>
public sealed class LocalFileStorage(string rootPath) : IFileStorage
{
    public Task<string> SaveAsync(string fileName, byte[] bytes, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(rootPath);
        var path = Path.Combine(rootPath, fileName);
        File.WriteAllBytes(path, bytes);
        return Task.FromResult(fileName);
    }
}
