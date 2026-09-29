using Khyout.Application.Abstractions;
using Khyout.Application.Common;
using Khyout.Application.Common.Cqrs;
using Microsoft.EntityFrameworkCore;

namespace Khyout.Application.Features.Companies;

public sealed record VerifyCompanyCommand(Guid CompanyId, bool Approved) : ICommand<CompanyDto>;

public sealed class VerifyCompanyCommandHandler(
    IAppDbContext db,
    ICurrentUser currentUser,
    IDateTimeProvider clock)
    : ICommandHandler<VerifyCompanyCommand, CompanyDto>
{
    public async Task<CompanyDto> Handle(VerifyCompanyCommand command, CancellationToken cancellationToken)
    {
        var adminId = currentUser.UserId
            ?? throw new ForbiddenException("Authentication required.");

        var company = await db.Companies.FirstOrDefaultAsync(c => c.Id == command.CompanyId, cancellationToken)
            ?? throw new NotFoundException("Company not found.");

        if (command.Approved)
        {
            company.Verify(adminId, clock.UtcNow);
        }
        else
        {
            company.Reject(clock.UtcNow);
        }

        await db.SaveChangesAsync(cancellationToken);
        return CompanyDto.From(company);
    }
}
