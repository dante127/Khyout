using Khyout.Domain.Common;
using Khyout.Domain.Enums;

namespace Khyout.Domain.Entities;

/// <summary>
/// Durable notification queue entry. Written in the same transaction as the state
/// change it announces; dispatched to Telegram with retry/backoff.
/// </summary>
public class OutboxMessage
{
    public const int MaxAttempts = 10;

    private OutboxMessage() { } // EF Core

    public Guid Id { get; private set; }
    public OutboxMessageType Type { get; private set; }

    /// <summary>JSON payload used to render the notification.</summary>
    public string Payload { get; private set; } = null!;

    /// <summary>Recipient reference (user or company id) for routing.</summary>
    public string TargetRef { get; private set; } = null!;

    public short Attempts { get; private set; }
    public DateTimeOffset NextAttemptAt { get; private set; }
    public OutboxMessageStatus Status { get; private set; }
    public string? LastError { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? SentAt { get; private set; }

    public static OutboxMessage Create(OutboxMessageType type, string payload, string targetRef, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new DomainRuleException("outbox_payload_required", "Payload is required.");
        }

        if (string.IsNullOrWhiteSpace(targetRef))
        {
            throw new DomainRuleException("outbox_target_required", "Target reference is required.");
        }

        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = type,
            Payload = payload,
            TargetRef = targetRef.Trim(),
            Attempts = 0,
            NextAttemptAt = now,
            Status = OutboxMessageStatus.Pending,
            CreatedAt = now
        };
    }

    public void MarkSent(DateTimeOffset now)
    {
        Status = OutboxMessageStatus.Sent;
        SentAt = now;
        LastError = null;
    }

    public void RegisterFailure(string error, DateTimeOffset nextAttemptAt)
    {
        Attempts++;
        LastError = error;
        NextAttemptAt = nextAttemptAt;
        if (Attempts >= MaxAttempts)
        {
            Status = OutboxMessageStatus.Failed;
        }
    }
}
