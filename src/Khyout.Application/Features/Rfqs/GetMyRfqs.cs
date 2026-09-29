using Khyout.Application.Abstractions;
using Khyout.Application.Common;
using Khyout.Application.Common.Cqrs;
using Khyout.Domain.Common;
using Khyout.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Khyout.Application.Features.Rfqs;

public sealed record GetMyRfqsQuery(int PageNumber = 1, int PageSize = 20, string? Status = null)
    : IQuery<PagedResult<RfqSummaryDto>>;

public sealed class GetMyRfqsQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    : IQueryHandler<GetMyRfqsQuery, PagedResult<RfqSummaryDto>>
{
    public async Task<PagedResult<RfqSummaryDto>> Handle(GetMyRfqsQuery query, CancellationToken cancellationToken)
    {
        var companyId = currentUser.CompanyId
            ?? throw new ForbiddenException("No company is associated with this account.");

        var (page, size) = Pagination.Normalize(query.PageNumber, query.PageSize);

        var mine = db.RfqRequests.Where(r => r.BuyerCompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!Enum.TryParse<RfqStatus>(query.Status, ignoreCase: true, out var status))
            {
                throw new DomainRuleException("rfq_status_invalid", $"'{query.Status}' is not a valid RFQ status.");
            }

            mine = mine.Where(r => r.Status == status);
        }

        var total = await mine.LongCountAsync(cancellationToken);

        var items = await mine
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(r => new RfqSummaryDto(
                r.Id,
                r.Title,
                r.CategoryId,
                r.Category.NameEn,
                r.QuantityNeeded,
                r.UnitOfMeasure.ToString(),
                r.ClosingDate,
                r.Status.ToString(),
                r.Quotations.Count,
                r.CreatedAt))
            .ToListAsync(cancellationToken);

        return PagedResult<RfqSummaryDto>.Create(items, page, size, total);
    }
}
