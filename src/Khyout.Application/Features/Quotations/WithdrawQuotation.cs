using FluentValidation;
using Khyout.Application.Abstractions;
using Khyout.Application.Common;
using Khyout.Application.Common.Cqrs;
using Microsoft.EntityFrameworkCore;

namespace Khyout.Application.Features.Quotations;

public sealed record WithdrawQuotationCommand(Guid QuotationId) : ICommand<Unit>;

public sealed class WithdrawQuotationCommandValidator : AbstractValidator<WithdrawQuotationCommand>
{
    public WithdrawQuotationCommandValidator()
    {
        RuleFor(c => c.QuotationId).NotEmpty();
    }
}

public sealed class WithdrawQuotationCommandHandler(IAppDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    : ICommandHandler<WithdrawQuotationCommand, Unit>
{
    public async Task<Unit> Handle(WithdrawQuotationCommand command, CancellationToken cancellationToken)
    {
        var companyId = currentUser.CompanyId
            ?? throw new ForbiddenException("No company is associated with this account.");

        var quotation = await db.RfqQuotations
            .FirstOrDefaultAsync(q => q.Id == command.QuotationId, cancellationToken)
            ?? throw new NotFoundException("Quotation not found.");

        if (quotation.SupplierCompanyId != companyId)
        {
            throw new ForbiddenException("Only the bidding supplier can withdraw this bid.");
        }

        quotation.Withdraw(clock.UtcNow);
        await db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
