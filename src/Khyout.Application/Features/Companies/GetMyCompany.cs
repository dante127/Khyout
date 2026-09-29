using Khyout.Application.Abstractions;
using Khyout.Application.Common;
using Khyout.Application.Common.Cqrs;
using Microsoft.EntityFrameworkCore;

namespace Khyout.Application.Features.Companies;

public sealed record GetMyCompanyQuery : IQuery<CompanyDto>;

public sealed class GetMyCompanyQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    : IQueryHandler<GetMyCompanyQuery, CompanyDto>
{
    public async Task<CompanyDto> Handle(GetMyCompanyQuery query, CancellationToken cancellationToken)
    {
        var companyId = currentUser.CompanyId
            ?? throw new ForbiddenException("No company is associated with this account.");

        var company = await db.Companies.FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken)
            ?? throw new NotFoundException("Company not found.");

        return CompanyDto.From(company);
    }
}
