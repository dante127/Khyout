using System.Text.Json;
using FluentValidation;
using Khyout.Application.Abstractions;
using Khyout.Application.Common;
using Khyout.Application.Common.Cqrs;
using Khyout.Domain.Entities;
using Khyout.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Khyout.Application.Features.Quotations;

public sealed record RejectQuotationCommand(Guid QuotationId) : ICommand<Unit>;

public sealed class RejectQuotationCommandValidator : AbstractValidator<RejectQuotationCommand>
{
    public RejectQuotationCommandValidator()
    {
        RuleFor(c => c.QuotationId).NotEmpty();
    }
}

public sealed class RejectQuotationCommandHandler(IAppDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    : ICommandHandler<RejectQuotationCommand, Unit>
{
    public async Task<Unit> Handle(RejectQuotationCommand command, CancellationToken cancellationToken)
    {
        var companyId = currentUser.CompanyId
            ?? throw new ForbiddenException("No company is associated with this account.");

        var quotation = await db.RfqQuotations
            .Include(q => q.RfqRequest)
            .FirstOrDefaultAsync(q => q.Id == command.QuotationId, cancellationToken)
            ?? throw new NotFoundException("Quotation not found.");

        if (quotation.RfqRequest.BuyerCompanyId != companyId)
        {
            throw new ForbiddenException("Only the RFQ owner can reject a bid.");
        }

        var now = clock.UtcNow;
        quotation.MarkRejected(now);

        db.OutboxMessages.Add(OutboxMessage.Create(
            OutboxMessageType.QuotationRejected,
            JsonSerializer.Serialize(new
            {
                quotationId = quotation.Id,
                rfqId = quotation.RfqRequestId,
                supplierCompanyId = quotation.SupplierCompanyId
            }),
            quotation.SupplierCompanyId.ToString(),
            now));

        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
