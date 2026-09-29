using Khyout.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khyout.Infrastructure.Persistence.Configurations;

public sealed class FabricAttributesConfiguration : IEntityTypeConfiguration<FabricAttributes>
{
    public void Configure(EntityTypeBuilder<FabricAttributes> builder)
    {
        builder.ToTable("fabric_attributes", t =>
        {
            t.HasCheckConstraint("ck_fabric_attributes_gsm", "gsm > 0");
            t.HasCheckConstraint("ck_fabric_attributes_gsm_tolerance", "gsm_tolerance_pct BETWEEN 0 AND 100");
            t.HasCheckConstraint("ck_fabric_attributes_width", "width_cm > 0");
            t.HasCheckConstraint("ck_fabric_attributes_weave",
                "weave_structure IN ('Plain', 'Twill', 'Satin', 'Knit', 'Denim')");
        });

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id).HasColumnName("id");
        builder.Property(a => a.ProductId).HasColumnName("product_id").IsRequired();
        builder.Property(a => a.Gsm).HasColumnName("gsm").IsRequired();
        builder.Property(a => a.GsmTolerancePct).HasColumnName("gsm_tolerance_pct").IsRequired();
        builder.Property(a => a.WeaveStructure).HasColumnName("weave_structure")
            .HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(a => a.WidthCm).HasColumnName("width_cm").IsRequired();
        builder.Property(a => a.ColorFamily).HasColumnName("color_family").HasMaxLength(50);
        builder.Property(a => a.WeightPerMeterG).HasColumnName("weight_per_meter_g");
        builder.Property(a => a.CareNotes).HasColumnName("care_notes").HasMaxLength(1000);

        builder.HasIndex(a => a.ProductId).IsUnique().HasDatabaseName("ux_fabric_attributes_product_id");
        builder.HasIndex(a => a.Gsm).HasDatabaseName("ix_fabric_attributes_gsm");
        builder.HasIndex(a => a.WeaveStructure).HasDatabaseName("ix_fabric_attributes_weave_structure");

        builder.HasOne(a => a.Product)
            .WithOne(p => p.Attributes)
            .HasForeignKey<FabricAttributes>(a => a.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
