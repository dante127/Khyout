using FluentValidation;
using Khyout.Application.Abstractions;
using Khyout.Application.Common;
using Khyout.Application.Common.Cqrs;
using Khyout.Application.Common.Security;
using Khyout.Domain.Common;
using Khyout.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Khyout.Application.Features.Auth;

public sealed record VerifyOtpCommand(string PhoneNumber, string Code) : ICommand<TokenPairDto>;

public sealed class VerifyOtpCommandValidator : AbstractValidator<VerifyOtpCommand>
{
    public VerifyOtpCommandValidator()
    {
        RuleFor(c => c.PhoneNumber).NotEmpty().MaximumLength(20);
        RuleFor(c => c.Code).NotEmpty().Length(6);
    }
}

public sealed class VerifyOtpCommandHandler(IAppDbContext db, OtpVerifier otpVerifier, ITokenService tokens)
    : ICommandHandler<VerifyOtpCommand, TokenPairDto>
{
    public async Task<TokenPairDto> Handle(VerifyOtpCommand command, CancellationToken cancellationToken)
    {
        var phone = OtpVerifier.NormalizePhone(command.PhoneNumber);

        await otpVerifier.VerifyAndConsumeAsync(phone, OtpPurpose.Login, command.Code, cancellationToken);

        var user = await db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phone, cancellationToken)
                   ?? throw new DomainRuleException(
                       "user_not_found",
                       "No account exists for this phone number. Complete onboarding first.");

        if (!user.IsActive)
        {
            throw new ForbiddenException("This account is disabled.");
        }

        var pair = await tokens.IssueAsync(user.Id, user.FullName, user.Role, user.CompanyId, cancellationToken);
        return TokenPairDto.From(pair);
    }
}
