using System.Security.Cryptography;
using FluentValidation;
using Khyout.Application.Abstractions;
using Khyout.Application.Common.Cqrs;
using Khyout.Application.Common.Security;
using Khyout.Domain.Entities;
using Khyout.Domain.Enums;

namespace Khyout.Application.Features.Auth;

public sealed record RequestOtpCommand(string PhoneNumber, OtpPurpose Purpose) : ICommand<Unit>;

public sealed class RequestOtpCommandValidator : AbstractValidator<RequestOtpCommand>
{
    public RequestOtpCommandValidator()
    {
        RuleFor(c => c.PhoneNumber).NotEmpty().MaximumLength(20);
        RuleFor(c => c.Purpose).IsInEnum();
    }
}

public sealed class RequestOtpCommandHandler(IAppDbContext db, IDateTimeProvider clock, ISmsSender sms)
    : ICommandHandler<RequestOtpCommand, Unit>
{
    public async Task<Unit> Handle(RequestOtpCommand command, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var phone = OtpVerifier.NormalizePhone(command.PhoneNumber);

        // Six-digit code; only its hash is stored. Verification always uses the newest code.
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        db.OtpCodes.Add(OtpCode.Create(phone, command.Purpose, OtpHasher.Hash(phone, code), now));
        await db.SaveChangesAsync(cancellationToken);

        await sms.SendAsync(
            phone,
            $"Your Khyout verification code is {code}. It expires in 5 minutes.",
            cancellationToken);

        return Unit.Value;
    }
}
