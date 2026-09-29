using System.Security.Cryptography;
using System.Text;
using Khyout.Application.Abstractions;
using Khyout.Domain.Common;
using Khyout.Domain.Entities;
using Khyout.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Khyout.Infrastructure.Identity;

/// <summary>
/// Issues JWT access tokens (15 min) and rotating refresh tokens (30 days, stored hashed).
/// JWT claims: sub (user id), name, role, companyId.
/// </summary>
public sealed class JwtTokenService(IAppDbContext db, IDateTimeProvider clock, IConfiguration configuration)
    : ITokenService
{
    private const int AccessTokenMinutes = 15;
    private const int RefreshTokenDays = 30;

    public async Task<TokenPair> IssueAsync(
        Guid userId,
        string fullName,
        UserRole role,
        Guid? companyId,
        CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        var accessToken = CreateAccessToken(userId, fullName, role, companyId, now);
        var refreshRaw = GenerateRefreshToken();
        var refreshToken = RefreshToken.Create(userId, HashToken(refreshRaw), now, TimeSpan.FromDays(RefreshTokenDays));

        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync(cancellationToken);

        return new TokenPair(accessToken, refreshRaw, now.AddMinutes(AccessTokenMinutes), refreshToken.ExpiresAt);
    }

    public async Task<TokenPair> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        var token = await db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == HashToken(refreshToken), cancellationToken)
            ?? throw new DomainRuleException("refresh_invalid", "Refresh token is not recognized.");

        if (!token.IsActive(now))
        {
            throw new DomainRuleException("refresh_expired", "Refresh token has expired or was revoked. Sign in again.");
        }

        var user = token.User;
        var newRaw = GenerateRefreshToken();
        var newToken = RefreshToken.Create(user.Id, HashToken(newRaw), now, TimeSpan.FromDays(RefreshTokenDays));

        db.RefreshTokens.Add(newToken);
        token.Revoke(now, newToken.Id);
        await db.SaveChangesAsync(cancellationToken);

        var accessToken = CreateAccessToken(user.Id, user.FullName, user.Role, user.CompanyId, now);
        return new TokenPair(accessToken, newRaw, now.AddMinutes(AccessTokenMinutes), newToken.ExpiresAt);
    }

    private string CreateAccessToken(Guid userId, string fullName, UserRole role, Guid? companyId, DateTimeOffset now)
    {
        var key = configuration["Auth:Jwt:SigningKey"]
            ?? throw new InvalidOperationException("Auth:Jwt:SigningKey is not configured.");

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = configuration["Auth:Jwt:Issuer"] ?? "khyout",
            Audience = configuration["Auth:Jwt:Audience"] ?? "khyout",
            Expires = now.AddMinutes(AccessTokenMinutes).UtcDateTime,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>
            {
                ["sub"] = userId.ToString(),
                ["name"] = fullName,
                ["role"] = role.ToString(),
                ["companyId"] = companyId?.ToString() ?? string.Empty
            }
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    private static string GenerateRefreshToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    private static string HashToken(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
