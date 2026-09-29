using Khyout.Application.Abstractions;
using Khyout.Application.Common;
using Khyout.Application.Common.Cqrs;
using Khyout.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Khyout.Application.Features.Companies;

public sealed record ListPendingCompaniesQuery(int PageNumber = 1, int PageSize = 20)
    : IQuery<PagedResult<CompanyDto>>;

public sealed class ListPendingCompaniesQueryHandler(IAppDbContext db)
    : IQueryHandler<ListPendingCompaniesQuery, PagedResult<CompanyDto>>
{
    public async Task<PagedResult<CompanyDto>> Handle(ListPendingCompaniesQuery query, CancellationToken cancellationToken)
    {
        var (page, size) = Pagination.Normalize(query.PageNumber, query.PageSize);

        var pending = db.Companies.Where(c => c.VerificationStatus == VerificationStatus.Pending);
        var total = await pending.LongCountAsync(cancellationToken);

        var items = await pending
            .OrderBy(c => c.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        return PagedResult<CompanyDto>.Create(items.Select(CompanyDto.From).ToList(), page, size, total);
    }
}
