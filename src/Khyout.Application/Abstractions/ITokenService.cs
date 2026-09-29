using Khyout.Domain.Enums;

namespace Khyout.Application.Abstractions;

/// <summary>Access + rotating refresh token pair.</summary>
public sealed record TokenPair(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset RefreshTokenExpiresAt);

public interface ITokenService
{
    Task<TokenPair> IssueAsync(
        Guid userId,
        string fullName,
        UserRole role,
        Guid? companyId,
        CancellationToken cancellationToken = default);

    /// <summary>Rotates the refresh token (old one is revoked and linked to the new one).</summary>
    Task<TokenPair> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
}
