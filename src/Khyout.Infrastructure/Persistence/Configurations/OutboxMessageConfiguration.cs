using Khyout.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khyout.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages", t =>
        {
            t.HasCheckConstraint("ck_outbox_messages_status", "status IN ('Pending', 'Sent', 'Failed')");
            t.HasCheckConstraint("ck_outbox_messages_attempts", "attempts >= 0");
            t.HasCheckConstraint("ck_outbox_messages_type",
                "type IN ('RfqCreated', 'QuotationSubmitted', 'QuotationAccepted', 'QuotationRejected', " +
                "'QuotationExpired', 'SampleRequested', 'SampleStatusChanged')");
        });

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id).HasColumnName("id");
        builder.Property(m => m.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(m => m.Payload).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
        builder.Property(m => m.TargetRef).HasColumnName("target_ref").HasMaxLength(100).IsRequired();
        builder.Property(m => m.Attempts).HasColumnName("attempts").IsRequired();
        builder.Property(m => m.NextAttemptAt).HasColumnName("next_attempt_at").IsRequired();
        builder.Property(m => m.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(m => m.LastError).HasColumnName("last_error").HasMaxLength(1000);
        builder.Property(m => m.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(m => m.SentAt).HasColumnName("sent_at");

        builder.HasIndex(m => new { m.Status, m.NextAttemptAt })
            .HasDatabaseName("ix_outbox_messages_dispatch");
        builder.HasIndex(m => m.CreatedAt).HasDatabaseName("ix_outbox_messages_created_at");
    }
}
