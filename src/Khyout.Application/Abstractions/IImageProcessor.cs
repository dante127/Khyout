namespace Khyout.Application.Abstractions;

/// <summary>Result of the media pipeline: WebP bytes within the size budget.</summary>
public sealed record ProcessedImage(byte[] Bytes, int WidthPx, int HeightPx, string ContentHash);

public interface IImageProcessor
{
    /// <summary>
    /// Decodes the uploaded image, resizes the longest edge to 1600 px and encodes WebP,
    /// iterating quality down until the result is at most 150 KB.
    /// </summary>
    Task<ProcessedImage> ProcessToWebPAsync(Stream input, CancellationToken cancellationToken = default);
}
