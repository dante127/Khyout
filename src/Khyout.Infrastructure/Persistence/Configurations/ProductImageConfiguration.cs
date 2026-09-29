using Khyout.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khyout.Infrastructure.Persistence.Configurations;

public sealed class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("product_images", t =>
        {
            t.HasCheckConstraint("ck_product_images_dimensions", "width_px > 0 AND height_px > 0 AND size_bytes > 0");
        });

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id).HasColumnName("id");
        builder.Property(i => i.ProductId).HasColumnName("product_id").IsRequired();
        builder.Property(i => i.StoragePath).HasColumnName("storage_path").HasMaxLength(300).IsRequired();
        builder.Property(i => i.ContentHash).HasColumnName("content_hash").HasMaxLength(64).IsRequired();
        builder.Property(i => i.WidthPx).HasColumnName("width_px").IsRequired();
        builder.Property(i => i.HeightPx).HasColumnName("height_px").IsRequired();
        builder.Property(i => i.SizeBytes).HasColumnName("size_bytes").IsRequired();
        builder.Property(i => i.SortOrder).HasColumnName("sort_order").IsRequired();

        builder.HasIndex(i => i.ContentHash).IsUnique().HasDatabaseName("ux_product_images_content_hash");
        builder.HasIndex(i => new { i.ProductId, i.SortOrder }).HasDatabaseName("ix_product_images_product_sort");

        builder.HasOne(i => i.Product)
            .WithMany(p => p.Images)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
