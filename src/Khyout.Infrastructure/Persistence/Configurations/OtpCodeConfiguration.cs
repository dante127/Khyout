using Khyout.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khyout.Infrastructure.Persistence.Configurations;

public sealed class OtpCodeConfiguration : IEntityTypeConfiguration<OtpCode>
{
    public void Configure(EntityTypeBuilder<OtpCode> builder)
    {
        builder.ToTable("otp_codes", t =>
        {
            t.HasCheckConstraint("ck_otp_codes_purpose", "purpose IN ('Login', 'Onboarding')");
            t.HasCheckConstraint("ck_otp_codes_attempts", "attempts_made >= 0");
        });

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id).HasColumnName("id");
        builder.Property(o => o.PhoneNumber).HasColumnName("phone_number").HasMaxLength(20).IsRequired();
        builder.Property(o => o.Purpose).HasColumnName("purpose").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(o => o.CodeHash).HasColumnName("code_hash").HasMaxLength(128).IsRequired();
        builder.Property(o => o.AttemptsMade).HasColumnName("attempts_made").IsRequired();
        builder.Property(o => o.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(o => o.ConsumedAt).HasColumnName("consumed_at");
        builder.Property(o => o.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(o => new { o.PhoneNumber, o.Purpose, o.ExpiresAt })
            .HasDatabaseName("ix_otp_codes_lookup");
        builder.HasIndex(o => o.ExpiresAt).HasDatabaseName("ix_otp_codes_expires_at");
    }
}
