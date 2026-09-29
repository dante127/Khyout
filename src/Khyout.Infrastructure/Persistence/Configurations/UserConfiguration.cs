using Khyout.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khyout.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", t => t.HasCheckConstraint(
            "ck_users_role", "role IN ('Buyer', 'Supplier', 'Admin')"));

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id).HasColumnName("id");
        builder.Property(u => u.PhoneNumber).HasColumnName("phone_number").HasMaxLength(20).IsRequired();
        builder.Property(u => u.FullName).HasColumnName("full_name").HasMaxLength(200).IsRequired();
        builder.Property(u => u.Role).HasColumnName("role").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(u => u.CompanyId).HasColumnName("company_id");
        builder.Property(u => u.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(u => u.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(u => u.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Property(u => u.TelegramChatId).HasColumnName("telegram_chat_id").HasMaxLength(32);
        builder.Property(u => u.TelegramLinkCode).HasColumnName("telegram_link_code").HasMaxLength(64);
        builder.Property(u => u.TelegramLinkCodeExpiresAt).HasColumnName("telegram_link_code_expires_at");

        builder.HasIndex(u => u.PhoneNumber).IsUnique().HasDatabaseName("ux_users_phone_number");
        builder.HasIndex(u => u.CompanyId).HasDatabaseName("ix_users_company_id");
        builder.HasIndex(u => u.Role).HasDatabaseName("ix_users_role");

        builder.HasOne(u => u.Company)
            .WithMany(c => c.Users)
            .HasForeignKey(u => u.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
