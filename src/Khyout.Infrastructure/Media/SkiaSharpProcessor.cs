using System.Security.Cryptography;
using Khyout.Application.Abstractions;
using Khyout.Domain.Common;
using SkiaSharp;

namespace Khyout.Infrastructure.Media;

/// <summary>
/// SkiaSharp-based media pipeline: decode → resize (longest edge 1600 px) → WebP with
/// quality iteration down to the 150 KB budget (Phase 1 spec, constraint C2).
/// SkiaSharp is MIT-licensed — ImageSharp 4.x requires a commercial build-time license,
/// and the IImageProcessor abstraction keeps this implementation swappable.
/// </summary>
public sealed class SkiaSharpProcessor : IImageProcessor
{
    private const int MaxLongestEdge = 1600;
    private const int MaxBytes = 150 * 1024;

    public Task<ProcessedImage> ProcessToWebPAsync(Stream input, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var source = SKBitmap.Decode(input)
            ?? throw new DomainRuleException(
                "image_decode_failed",
                "The uploaded file is not a supported image (JPEG, PNG or WebP).");

        var scale = Math.Min(MaxLongestEdge / (double)Math.Max(source.Width, source.Height), 1.0);

        SKBitmap? resized = null;
        try
        {
            var bitmap = source;
            if (scale < 1.0)
            {
                var targetWidth = Math.Max(1, (int)Math.Round(source.Width * scale));
                var targetHeight = Math.Max(1, (int)Math.Round(source.Height * scale));

                resized = new SKBitmap(new SKImageInfo(targetWidth, targetHeight));
                using var canvas = new SKCanvas(resized);
                canvas.DrawBitmap(source, new SKRect(0, 0, targetWidth, targetHeight),
                    new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
                bitmap = resized;
            }

            var bytes = Encode(bitmap, 82);
            var quality = 72;
            while (bytes.Length > MaxBytes && quality >= 30)
            {
                bytes = Encode(bitmap, quality);
                quality -= 10;
            }

            var hash = Convert.ToHexString(SHA256.HashData(bytes));
            return Task.FromResult(new ProcessedImage(bytes, bitmap.Width, bitmap.Height, hash));
        }
        finally
        {
            resized?.Dispose();
        }
    }

    private static byte[] Encode(SKBitmap bitmap, int quality)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Webp, quality);
        return data.ToArray();
    }
}
