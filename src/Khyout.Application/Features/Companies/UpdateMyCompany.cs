using FluentValidation;
using Khyout.Application.Abstractions;
using Khyout.Application.Common;
using Khyout.Application.Common.Cqrs;
using Microsoft.EntityFrameworkCore;

namespace Khyout.Application.Features.Companies;

public sealed record UpdateMyCompanyCommand(string Name, string City, string? Address, string? Bio) : ICommand<CompanyDto>;

public sealed class UpdateMyCompanyCommandValidator : AbstractValidator<UpdateMyCompanyCommand>
{
    public UpdateMyCompanyCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
        RuleFor(c => c.City).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Address).MaximumLength(400);
        RuleFor(c => c.Bio).MaximumLength(2000);
    }
}

public sealed class UpdateMyCompanyCommandHandler(
    IAppDbContext db,
    ICurrentUser currentUser,
    IDateTimeProvider clock)
    : ICommandHandler<UpdateMyCompanyCommand, CompanyDto>
{
    public async Task<CompanyDto> Handle(UpdateMyCompanyCommand command, CancellationToken cancellationToken)
    {
        var companyId = currentUser.CompanyId
            ?? throw new ForbiddenException("No company is associated with this account.");

        var company = await db.Companies.FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken)
            ?? throw new NotFoundException("Company not found.");

        company.UpdateProfile(command.Name, command.City, command.Address, command.Bio, clock.UtcNow);
        await db.SaveChangesAsync(cancellationToken);

        return CompanyDto.From(company);
    }
}
