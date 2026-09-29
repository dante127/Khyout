using Khyout.Application.Abstractions;
using Khyout.Application.Common;
using Khyout.Application.Common.Cqrs;
using Khyout.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Khyout.Application.Features.Rfqs;

public sealed record GetRfqDetailQuery(Guid RfqId) : IQuery<RfqDetailResult>;

/// <summary>
/// Role-aware RFQ view. Bid data is never included here — bids are exposed only
/// through GetQuotationsForRfq, which enforces the platform's blind-bidding rule.
/// </summary>
public sealed record RfqDetailResult(
    RfqDetailDto Rfq,
    bool IsBuyerOwner,
    bool CanReceiveBids,
    int BidCount,
    MyQuotationDto? MyQuotation);

public sealed class GetRfqDetailQueryHandler(IAppDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    : IQueryHandler<GetRfqDetailQuery, RfqDetailResult>
{
    public async Task<RfqDetailResult> Handle(GetRfqDetailQuery query, CancellationToken cancellationToken)
    {
        var rfq = await db.RfqRequests
            .AsNoTracking()
            .Include(r => r.Category)
            .Include(r => r.BuyerCompany)
            .Include(r => r.Quotations)
            .FirstOrDefaultAsync(r => r.Id == query.RfqId, cancellationToken)
            ?? throw new NotFoundException("RFQ not found.");

        var isBuyerOwner = currentUser.CompanyId is { } companyId && companyId == rfq.BuyerCompanyId;
        var isAdmin = currentUser.Role == UserRole.Admin;
        var isSupplier = currentUser.Role == UserRole.Supplier;

        if (!isBuyerOwner && !isAdmin && !isSupplier)
        {
            throw new ForbiddenException("Only buyers, suppliers and admins can view this RFQ.");
        }

        MyQuotationDto? myQuotation = null;
        if (isSupplier && currentUser.CompanyId is { } supplierCompanyId)
        {
            var own = rfq.Quotations.FirstOrDefault(q => q.SupplierCompanyId == supplierCompanyId);
            if (own is not null)
            {
                myQuotation = QuotationMapping.ToMine(own);
            }
        }

        // Suppliers only learn about their own participation; buyers/admins see the full count.
        var bidCount = isBuyerOwner || isAdmin
            ? rfq.Quotations.Count
            : rfq.Quotations.Count(q => q.SupplierCompanyId == currentUser.CompanyId);

        return new RfqDetailResult(
            RfqMapping.ToDetail(rfq),
            isBuyerOwner,
            rfq.CanReceiveBids(clock.UtcNow),
            bidCount,
            myQuotation);
    }
}
