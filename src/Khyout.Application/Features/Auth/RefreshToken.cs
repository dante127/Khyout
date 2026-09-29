using FluentValidation;
using Khyout.Application.Abstractions;
using Khyout.Application.Common.Cqrs;

namespace Khyout.Application.Features.Auth;

public sealed record RefreshTokenCommand(string RefreshToken) : ICommand<TokenPairDto>;

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(c => c.RefreshToken).NotEmpty();
    }
}

public sealed class RefreshTokenCommandHandler(ITokenService tokens)
    : ICommandHandler<RefreshTokenCommand, TokenPairDto>
{
    public async Task<TokenPairDto> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var pair = await tokens.RefreshAsync(command.RefreshToken, cancellationToken);
        return TokenPairDto.From(pair);
    }
}
