using Khyout.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khyout.Infrastructure.Persistence.Configurations;

public sealed class RfqQuotationConfiguration : IEntityTypeConfiguration<RfqQuotation>
{
    public void Configure(EntityTypeBuilder<RfqQuotation> builder)
    {
        builder.ToTable("rfq_quotations", t =>
        {
            t.HasCheckConstraint("ck_rfq_quotations_status",
                "status IN ('Submitted', 'Accepted', 'Rejected', 'Expired', 'Withdrawn')");
            t.HasCheckConstraint("ck_rfq_quotations_price", "unit_price > 0");
            t.HasCheckConstraint("ck_rfq_quotations_lead_time", "lead_time_days >= 0");
        });

        builder.HasKey(q => q.Id);

        builder.Property(q => q.Id).HasColumnName("id");
        builder.Property(q => q.RfqRequestId).HasColumnName("rfq_request_id").IsRequired();
        builder.Property(q => q.SupplierCompanyId).HasColumnName("supplier_company_id").IsRequired();
        builder.Property(q => q.UnitPrice).HasColumnName("unit_price").HasPrecision(14, 2).IsRequired();
        builder.Property(q => q.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
        builder.Property(q => q.ValidUntil).HasColumnName("valid_until").IsRequired();
        builder.Property(q => q.LeadTimeDays).HasColumnName("lead_time_days").IsRequired();
        builder.Property(q => q.Note).HasColumnName("note").HasMaxLength(2000);
        builder.Property(q => q.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(q => q.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(q => q.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasIndex(q => new { q.RfqRequestId, q.SupplierCompanyId })
            .IsUnique()
            .HasDatabaseName("ux_rfq_quotations_rfq_supplier");
        builder.HasIndex(q => new { q.RfqRequestId, q.Status })
            .HasDatabaseName("ix_rfq_quotations_rfq_status");
        builder.HasIndex(q => new { q.SupplierCompanyId, q.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("ix_rfq_quotations_supplier_created");
        builder.HasIndex(q => new { q.ValidUntil, q.Status })
            .HasDatabaseName("ix_rfq_quotations_valid_until_status");

        builder.HasOne(q => q.RfqRequest)
            .WithMany(r => r.Quotations)
            .HasForeignKey(q => q.RfqRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(q => q.SupplierCompany)
            .WithMany()
            .HasForeignKey(q => q.SupplierCompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
