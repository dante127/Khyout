using Khyout.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khyout.Infrastructure.Persistence.Configurations;

public sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("companies", t =>
        {
            t.HasCheckConstraint("ck_companies_type", "type IN ('Buyer', 'Supplier')");
            t.HasCheckConstraint("ck_companies_verification_status",
                "verification_status IN ('Pending', 'Verified', 'Rejected')");
        });

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(c => c.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(c => c.City).HasColumnName("city").HasMaxLength(100).IsRequired();
        builder.Property(c => c.Address).HasColumnName("address").HasMaxLength(400);
        builder.Property(c => c.Bio).HasColumnName("bio");
        builder.Property(c => c.VerificationStatus).HasColumnName("verification_status")
            .HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(c => c.VerifiedAt).HasColumnName("verified_at");
        builder.Property(c => c.VerifiedByUserId).HasColumnName("verified_by_user_id");
        builder.Property(c => c.LogoPath).HasColumnName("logo_path").HasMaxLength(300);
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasIndex(c => c.City).HasDatabaseName("ix_companies_city");
        builder.HasIndex(c => c.VerificationStatus).HasDatabaseName("ix_companies_verification_status");
        builder.HasIndex(c => c.Name).HasDatabaseName("ix_companies_name");

        // FK without navigation: the verifying admin user.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(c => c.VerifiedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
