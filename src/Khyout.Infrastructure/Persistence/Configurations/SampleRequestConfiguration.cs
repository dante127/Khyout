using Khyout.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khyout.Infrastructure.Persistence.Configurations;

public sealed class SampleRequestConfiguration : IEntityTypeConfiguration<SampleRequest>
{
    public void Configure(EntityTypeBuilder<SampleRequest> builder)
    {
        builder.ToTable("sample_requests", t =>
        {
            t.HasCheckConstraint("ck_sample_requests_status",
                "status IN ('Requested', 'Approved', 'Shipped', 'Received', 'Rejected')");
            t.HasCheckConstraint("ck_sample_requests_quantity", "quantity > 0");
        });

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id).HasColumnName("id");
        builder.Property(s => s.BuyerCompanyId).HasColumnName("buyer_company_id").IsRequired();
        builder.Property(s => s.SupplierCompanyId).HasColumnName("supplier_company_id").IsRequired();
        builder.Property(s => s.ProductId).HasColumnName("product_id").IsRequired();
        builder.Property(s => s.RfqQuotationId).HasColumnName("rfq_quotation_id");
        builder.Property(s => s.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(s => s.Quantity).HasColumnName("quantity").HasPrecision(14, 2).IsRequired();
        builder.Property(s => s.DeliveryCity).HasColumnName("delivery_city").HasMaxLength(100);
        builder.Property(s => s.Note).HasColumnName("note").HasMaxLength(1000);
        builder.Property(s => s.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasIndex(s => new { s.BuyerCompanyId, s.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("ix_sample_requests_buyer_created");
        builder.HasIndex(s => new { s.SupplierCompanyId, s.Status })
            .HasDatabaseName("ix_sample_requests_supplier_status");

        builder.HasOne(s => s.BuyerCompany)
            .WithMany()
            .HasForeignKey(s => s.BuyerCompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.SupplierCompany)
            .WithMany()
            .HasForeignKey(s => s.SupplierCompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Product)
            .WithMany()
            .HasForeignKey(s => s.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.RfqQuotation)
            .WithMany()
            .HasForeignKey(s => s.RfqQuotationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
