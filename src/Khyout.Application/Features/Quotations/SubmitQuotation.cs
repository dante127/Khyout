using System.Text.Json;
using FluentValidation;
using Khyout.Application.Abstractions;
using Khyout.Application.Common;
using Khyout.Application.Common.Cqrs;
using Khyout.Application.Features.Rfqs;
using Khyout.Domain.Common;
using Khyout.Domain.Entities;
using Khyout.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Khyout.Application.Features.Quotations;

public sealed record SubmitQuotationCommand(
    Guid RfqId,
    decimal UnitPrice,
    string Currency,
    DateTimeOffset ValidUntil,
    int LeadTimeDays,
    string? Note) : ICommand<MyQuotationDto>;

public sealed class SubmitQuotationCommandValidator : AbstractValidator<SubmitQuotationCommand>
{
    public SubmitQuotationCommandValidator()
    {
        RuleFor(c => c.RfqId).NotEmpty();
        RuleFor(c => c.UnitPrice).GreaterThan(0);
        RuleFor(c => c.Currency).NotEmpty().Length(3);
        RuleFor(c => c.LeadTimeDays).GreaterThanOrEqualTo(0);
        RuleFor(c => c.Note).MaximumLength(2000);
    }
}

public sealed class SubmitQuotationCommandHandler(IAppDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    : ICommandHandler<SubmitQuotationCommand, MyQuotationDto>
{
    public async Task<MyQuotationDto> Handle(SubmitQuotationCommand command, CancellationToken cancellationToken)
    {
        var companyId = currentUser.CompanyId
            ?? throw new ForbiddenException("No company is associated with this account.");

        var now = clock.UtcNow;

        var rfq = await db.RfqRequests
            .Include(r => r.Quotations)
            .FirstOrDefaultAsync(r => r.Id == command.RfqId, cancellationToken)
            ?? throw new NotFoundException("RFQ not found.");

        if (!rfq.CanReceiveBids(now))
        {
            throw new DomainRuleException("rfq_closed", "This RFQ is no longer accepting bids.");
        }

        var existing = rfq.Quotations.FirstOrDefault(q => q.SupplierCompanyId == companyId);

        RfqQuotation quotation;
        if (existing is not null)
        {
            existing.Update(
                command.UnitPrice,
                command.Currency,
                command.ValidUntil,
                command.LeadTimeDays,
                command.Note,
                now);
            quotation = existing;
        }
        else
        {
            quotation = RfqQuotation.Create(
                command.RfqId,
                companyId,
                command.UnitPrice,
                command.Currency,
                command.ValidUntil,
                command.LeadTimeDays,
                command.Note,
                now);
            db.RfqQuotations.Add(quotation);
        }

        db.OutboxMessages.Add(OutboxMessage.Create(
            OutboxMessageType.QuotationSubmitted,
            JsonSerializer.Serialize(new
            {
                quotationId = quotation.Id,
                rfqId = rfq.Id,
                buyerCompanyId = rfq.BuyerCompanyId
            }),
            rfq.BuyerCompanyId.ToString(),
            now));

        await db.SaveChangesAsync(cancellationToken);
        return QuotationMapping.ToMine(quotation);
    }
}
