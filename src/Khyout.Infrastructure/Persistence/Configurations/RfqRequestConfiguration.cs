using Khyout.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khyout.Infrastructure.Persistence.Configurations;

public sealed class RfqRequestConfiguration : IEntityTypeConfiguration<RfqRequest>
{
    public void Configure(EntityTypeBuilder<RfqRequest> builder)
    {
        builder.ToTable("rfq_requests", t =>
        {
            t.HasCheckConstraint("ck_rfq_requests_status", "status IN ('Open', 'Awarded', 'Cancelled', 'Expired')");
            t.HasCheckConstraint("ck_rfq_requests_quantity", "quantity_needed > 0");
        });

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.BuyerCompanyId).HasColumnName("buyer_company_id").IsRequired();
        builder.Property(r => r.CategoryId).HasColumnName("category_id").IsRequired();
        builder.Property(r => r.Title).HasColumnName("title").HasMaxLength(300).IsRequired();
        builder.Property(r => r.Description).HasColumnName("description").HasMaxLength(4000);
        builder.Property(r => r.QuantityNeeded).HasColumnName("quantity_needed").HasPrecision(14, 2).IsRequired();
        builder.Property(r => r.UnitOfMeasure).HasColumnName("unit_of_measure")
            .HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(r => r.TargetDeliveryDate).HasColumnName("target_delivery_date").IsRequired();
        builder.Property(r => r.ClosingDate).HasColumnName("closing_date").IsRequired();
        builder.Property(r => r.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(r => r.AcceptedQuotationId).HasColumnName("accepted_quotation_id");
        builder.Property(r => r.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasIndex(r => new { r.BuyerCompanyId, r.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("ix_rfq_requests_buyer_created");
        builder.HasIndex(r => new { r.CategoryId, r.Status })
            .HasDatabaseName("ix_rfq_requests_category_status");
        builder.HasIndex(r => new { r.Status, r.ClosingDate })
            .HasDatabaseName("ix_rfq_requests_status_closing");

        builder.HasOne(r => r.BuyerCompany)
            .WithMany()
            .HasForeignKey(r => r.BuyerCompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Category)
            .WithMany()
            .HasForeignKey(r => r.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.AcceptedQuotation)
            .WithMany()
            .HasForeignKey(r => r.AcceptedQuotationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
