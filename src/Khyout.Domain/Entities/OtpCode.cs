using Khyout.Domain.Common;
using Khyout.Domain.Enums;

namespace Khyout.Domain.Entities;

/// <summary>
/// One-time password issued for phone-based login/onboarding. Not linked to a user
/// row because it exists before registration completes.
/// </summary>
public class OtpCode
{
    public const int MaxAttempts = 5;
    public static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);

    private OtpCode() { } // EF Core

    public Guid Id { get; private set; }
    public string PhoneNumber { get; private set; } = null!;
    public OtpPurpose Purpose { get; private set; }

    /// <summary>Hash of the code. Plaintext codes are never persisted.</summary>
    public string CodeHash { get; private set; } = null!;

    public short AttemptsMade { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? ConsumedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static OtpCode Create(string phoneNumber, OtpPurpose purpose, string codeHash, DateTimeOffset now, TimeSpan? ttl = null)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new DomainRuleException("otp_phone_required", "Phone number is required.");
        }

        if (string.IsNullOrWhiteSpace(codeHash))
        {
            throw new DomainRuleException("otp_hash_required", "Code hash is required.");
        }

        return new OtpCode
        {
            Id = Guid.NewGuid(),
            PhoneNumber = phoneNumber.Trim(),
            Purpose = purpose,
            CodeHash = codeHash,
            AttemptsMade = 0,
            ExpiresAt = now.Add(ttl ?? DefaultTtl),
            CreatedAt = now
        };
    }

    public bool IsUsable(DateTimeOffset now) =>
        ConsumedAt is null && ExpiresAt > now && AttemptsMade < MaxAttempts;

    public void RegisterFailedAttempt() => AttemptsMade++;

    public void MarkConsumed(DateTimeOffset now)
    {
        if (!IsUsable(now))
        {
            throw new DomainRuleException("otp_not_usable", "This code is no longer usable.");
        }

        ConsumedAt = now;
    }
}
