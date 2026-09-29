using Khyout.Application.Abstractions;
using Khyout.Application.Common;
using Khyout.Application.Common.Cqrs;
using Khyout.Application.Features.Rfqs;
using Khyout.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Khyout.Application.Features.Quotations;

public sealed record GetQuotationsForRfqQuery(Guid RfqId, int PageNumber = 1, int PageSize = 20)
    : IQuery<PagedResult<RfqBidSummaryDto>>;

/// <summary>
/// Blind-bidding rule: only the RFQ owner (buyer) and admins may see all bids;
/// a supplier can only ever see their own bid.
/// </summary>
public sealed class GetQuotationsForRfqQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    : IQueryHandler<GetQuotationsForRfqQuery, PagedResult<RfqBidSummaryDto>>
{
    public async Task<PagedResult<RfqBidSummaryDto>> Handle(
        GetQuotationsForRfqQuery query,
        CancellationToken cancellationToken)
    {
        var rfq = await db.RfqRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == query.RfqId, cancellationToken)
            ?? throw new NotFoundException("RFQ not found.");

        var companyId = currentUser.CompanyId;
        var isAdmin = currentUser.Role == UserRole.Admin;
        var isOwner = currentUser.Role == UserRole.Buyer && companyId is { } c && c == rfq.BuyerCompanyId;

        IQueryable<Domain.Entities.RfqQuotation> bids;
        if (isAdmin || isOwner)
        {
            bids = db.RfqQuotations.Where(q => q.RfqRequestId == query.RfqId);
        }
        else if (currentUser.Role == UserRole.Supplier && companyId is { } supplierCompanyId)
        {
            bids = db.RfqQuotations.Where(q =>
                q.RfqRequestId == query.RfqId && q.SupplierCompanyId == supplierCompanyId);
        }
        else
        {
            throw new ForbiddenException("You are not allowed to view bids for this RFQ.");
        }

        var (page, size) = Pagination.Normalize(query.PageNumber, query.PageSize);
        var total = await bids.LongCountAsync(cancellationToken);

        var items = await bids
            .OrderBy(q => q.UnitPrice)
            .ThenBy(q => q.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(q => new RfqBidSummaryDto(
                q.Id,
                q.SupplierCompanyId,
                q.SupplierCompany.Name,
                q.UnitPrice,
                q.Currency,
                q.ValidUntil,
                q.LeadTimeDays,
                q.Note,
                q.Status.ToString(),
                q.CreatedAt))
            .ToListAsync(cancellationToken);

        return PagedResult<RfqBidSummaryDto>.Create(items, page, size, total);
    }
}
