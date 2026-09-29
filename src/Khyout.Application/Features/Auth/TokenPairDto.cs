using Khyout.Application.Abstractions;

namespace Khyout.Application.Features.Auth;

public sealed record TokenPairDto(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset RefreshTokenExpiresAt)
{
    public static TokenPairDto From(TokenPair pair) =>
        new(pair.AccessToken, pair.RefreshToken, pair.AccessTokenExpiresAt, pair.RefreshTokenExpiresAt);
}
