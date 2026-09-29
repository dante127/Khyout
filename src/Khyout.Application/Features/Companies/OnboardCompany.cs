using FluentValidation;
using Khyout.Application.Abstractions;
using Khyout.Application.Common.Cqrs;
using Khyout.Application.Common.Security;
using Khyout.Application.Features.Auth;
using Khyout.Domain.Common;
using Khyout.Domain.Entities;
using Khyout.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Khyout.Application.Features.Companies;

public sealed record OnboardCompanyCommand(
    string PhoneNumber,
    string OtpCode,
    string FullName,
    string CompanyName,
    CompanyType CompanyType,
    string City,
    string? Address,
    string? Bio) : ICommand<TokenPairDto>;

public sealed class OnboardCompanyCommandValidator : AbstractValidator<OnboardCompanyCommand>
{
    public OnboardCompanyCommandValidator()
    {
        RuleFor(c => c.PhoneNumber).NotEmpty().MaximumLength(20);
        RuleFor(c => c.OtpCode).NotEmpty().Length(6);
        RuleFor(c => c.FullName).NotEmpty().MaximumLength(200);
        RuleFor(c => c.CompanyName).NotEmpty().MaximumLength(200);
        RuleFor(c => c.CompanyType).IsInEnum();
        RuleFor(c => c.City).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Address).MaximumLength(400);
        RuleFor(c => c.Bio).MaximumLength(2000);
    }
}

public sealed class OnboardCompanyCommandHandler(
    IAppDbContext db,
    OtpVerifier otpVerifier,
    ITokenService tokens,
    IDateTimeProvider clock)
    : ICommandHandler<OnboardCompanyCommand, TokenPairDto>
{
    public async Task<TokenPairDto> Handle(OnboardCompanyCommand command, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var phone = OtpVerifier.NormalizePhone(command.PhoneNumber);

        if (await db.Users.AnyAsync(u => u.PhoneNumber == phone, cancellationToken))
        {
            throw new DomainRuleException(
                "user_already_exists",
                "An account already exists for this phone number. Log in instead.");
        }

        await otpVerifier.VerifyAndConsumeAsync(phone, OtpPurpose.Onboarding, command.OtpCode, cancellationToken);

        var company = Company.Create(
            command.CompanyName,
            command.CompanyType,
            command.City,
            command.Address,
            command.Bio,
            now);

        var role = command.CompanyType == CompanyType.Supplier ? UserRole.Supplier : UserRole.Buyer;
        var user = User.Create(phone, command.FullName, role, company.Id, now);

        db.Companies.Add(company);
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        var pair = await tokens.IssueAsync(user.Id, user.FullName, user.Role, user.CompanyId, cancellationToken);
        return TokenPairDto.From(pair);
    }
}
