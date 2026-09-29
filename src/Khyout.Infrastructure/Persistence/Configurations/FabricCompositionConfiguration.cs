using Khyout.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khyout.Infrastructure.Persistence.Configurations;

public sealed class FabricCompositionConfiguration : IEntityTypeConfiguration<FabricComposition>
{
    public void Configure(EntityTypeBuilder<FabricComposition> builder)
    {
        builder.ToTable("fabric_composition", t =>
        {
            t.HasCheckConstraint("ck_fabric_composition_percentage", "percentage BETWEEN 0 AND 100");
        });

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.ProductId).HasColumnName("product_id").IsRequired();
        builder.Property(c => c.FiberType).HasColumnName("fiber_type")
            .HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(c => c.Percentage).HasColumnName("percentage").HasPrecision(5, 2).IsRequired();

        builder.HasIndex(c => new { c.ProductId, c.FiberType })
            .IsUnique()
            .HasDatabaseName("ux_fabric_composition_product_fiber");
        builder.HasIndex(c => new { c.FiberType, c.Percentage })
            .HasDatabaseName("ix_fabric_composition_fiber_percentage");

        builder.HasOne(c => c.Product)
            .WithMany(p => p.Composition)
            .HasForeignKey(c => c.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
