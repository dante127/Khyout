using Khyout.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khyout.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products", t =>
        {
            t.HasCheckConstraint("ck_products_status", "status IN ('Draft', 'Active', 'Archived')");
            t.HasCheckConstraint("ck_products_moq", "moq > 0");
            t.HasCheckConstraint("ck_products_unit", "unit_of_measure IN ('Meter', 'Kg', 'Roll', 'Yard')");
        });

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.SupplierCompanyId).HasColumnName("supplier_company_id").IsRequired();
        builder.Property(p => p.CategoryId).HasColumnName("category_id").IsRequired();
        builder.Property(p => p.Title).HasColumnName("title").HasMaxLength(300).IsRequired();
        builder.Property(p => p.Description).HasColumnName("description");
        builder.Property(p => p.Moq).HasColumnName("moq").HasPrecision(14, 2).IsRequired();
        builder.Property(p => p.UnitOfMeasure).HasColumnName("unit_of_measure")
            .HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(p => p.IndicativePrice).HasColumnName("indicative_price").HasPrecision(14, 2);
        builder.Property(p => p.Currency).HasColumnName("currency").HasMaxLength(3);
        builder.Property(p => p.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasIndex(p => p.SupplierCompanyId).HasDatabaseName("ix_products_supplier_company_id");
        builder.HasIndex(p => new { p.CategoryId, p.Status, p.CreatedAt })
            .IsDescending(false, false, true)
            .HasDatabaseName("ix_products_category_status_created");
        builder.HasIndex(p => p.Moq).HasDatabaseName("ix_products_moq");

        builder.HasOne(p => p.SupplierCompany)
            .WithMany()
            .HasForeignKey(p => p.SupplierCompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Category)
            .WithMany()
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
