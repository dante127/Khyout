using Khyout.Domain.Common;

namespace Khyout.Domain.Entities;

/// <summary>A WebP image attached to a product (target: 150 KB max, hash-deduped).</summary>
public class ProductImage
{
    private ProductImage() { } // EF Core

    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public Product Product { get; private set; } = null!;
    public string StoragePath { get; private set; } = null!;

    /// <summary>Content hash used for de-duplication.</summary>
    public string ContentHash { get; private set; } = null!;

    public int WidthPx { get; private set; }
    public int HeightPx { get; private set; }
    public int SizeBytes { get; private set; }
    public short SortOrder { get; private set; }

    public static ProductImage Create(
        Guid productId,
        string storagePath,
        string contentHash,
        int widthPx,
        int heightPx,
        int sizeBytes,
        short sortOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
        {
            throw new DomainRuleException("image_path_required", "Storage path is required.");
        }

        if (string.IsNullOrWhiteSpace(contentHash))
        {
            throw new DomainRuleException("image_hash_required", "Content hash is required.");
        }

        if (widthPx <= 0 || heightPx <= 0 || sizeBytes <= 0)
        {
            throw new DomainRuleException("image_dimensions_invalid", "Image dimensions and size must be positive.");
        }

        return new ProductImage
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            StoragePath = storagePath,
            ContentHash = contentHash,
            WidthPx = widthPx,
            HeightPx = heightPx,
            SizeBytes = sizeBytes,
            SortOrder = sortOrder
        };
    }
}
