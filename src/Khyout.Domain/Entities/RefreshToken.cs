using Khyout.Domain.Common;

namespace Khyout.Domain.Entities;

/// <summary>Rotating refresh token (stored hashed).</summary>
public class RefreshToken
{
    public static readonly TimeSpan DefaultTtl = TimeSpan.FromDays(30);

    private RefreshToken() { } // EF Core

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    /// <summary>SHA-256 hash of the token value; the raw token is returned once and never stored.</summary>
    public string TokenHash { get; private set; } = null!;

    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public Guid? ReplacedByTokenId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static RefreshToken Create(Guid userId, string tokenHash, DateTimeOffset now, TimeSpan? ttl = null)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new DomainRuleException("refresh_hash_required", "Token hash is required.");
        }

        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = now.Add(ttl ?? DefaultTtl),
            CreatedAt = now
        };
    }

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && ExpiresAt > now;

    public void Revoke(DateTimeOffset now, Guid? replacedByTokenId = null)
    {
        if (RevokedAt is not null)
        {
            throw new DomainRuleException("refresh_already_revoked", "Token is already revoked.");
        }

        RevokedAt = now;
        ReplacedByTokenId = replacedByTokenId;
    }
}
