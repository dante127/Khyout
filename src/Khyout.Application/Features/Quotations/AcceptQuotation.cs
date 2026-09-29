using System.Text.Json;
using FluentValidation;
using Khyout.Application.Abstractions;
using Khyout.Application.Common;
using Khyout.Application.Common.Cqrs;
using Khyout.Application.Features.Rfqs;
using Khyout.Domain.Entities;
using Khyout.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Khyout.Application.Features.Quotations;

public sealed record AcceptQuotationCommand(Guid QuotationId) : ICommand<RfqDetailDto>;

public sealed class AcceptQuotationCommandValidator : AbstractValidator<AcceptQuotationCommand>
{
    public AcceptQuotationCommandValidator()
    {
        RuleFor(c => c.QuotationId).NotEmpty();
    }
}

public sealed class AcceptQuotationCommandHandler(IAppDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    : ICommandHandler<AcceptQuotationCommand, RfqDetailDto>
{
    public async Task<RfqDetailDto> Handle(AcceptQuotationCommand command, CancellationToken cancellationToken)
    {
        var companyId = currentUser.CompanyId
            ?? throw new ForbiddenException("No company is associated with this account.");

        var quotation = await db.RfqQuotations
            .Include(q => q.RfqRequest).ThenInclude(r => r.Quotations)
            .FirstOrDefaultAsync(q => q.Id == command.QuotationId, cancellationToken)
            ?? throw new NotFoundException("Quotation not found.");

        var rfq = quotation.RfqRequest;
        if (rfq.BuyerCompanyId != companyId)
        {
            throw new ForbiddenException("Only the RFQ owner can accept a bid.");
        }

        var now = clock.UtcNow;

        // Guards: RFQ must be Open; the quotation must be Submitted and not expired (409 on failure).
        quotation.MarkAccepted(now);

        foreach (var other in rfq.Quotations.Where(q =>
                     q.Id != quotation.Id && q.Status == QuotationStatus.Submitted))
        {
            other.MarkRejected(now);
        }

        rfq.Award(quotation.Id, now);

        db.OutboxMessages.Add(OutboxMessage.Create(
            OutboxMessageType.QuotationAccepted,
            JsonSerializer.Serialize(new
            {
                quotationId = quotation.Id,
                rfqId = rfq.Id,
                supplierCompanyId = quotation.SupplierCompanyId
            }),
            quotation.SupplierCompanyId.ToString(),
            now));

        foreach (var rejected in rfq.Quotations.Where(q => q.Status == QuotationStatus.Rejected))
        {
            db.OutboxMessages.Add(OutboxMessage.Create(
                OutboxMessageType.QuotationRejected,
                JsonSerializer.Serialize(new
                {
                    quotationId = rejected.Id,
                    rfqId = rfq.Id,
                    supplierCompanyId = rejected.SupplierCompanyId
                }),
                rejected.SupplierCompanyId.ToString(),
                now));
        }

        await db.SaveChangesAsync(cancellationToken);

        var detail = await db.RfqRequests
            .Include(r => r.Category)
            .Include(r => r.BuyerCompany)
            .FirstAsync(r => r.Id == rfq.Id, cancellationToken);

        return RfqMapping.ToDetail(detail);
    }
}
