using FluentValidation;
using Khyout.Application.Abstractions;
using Khyout.Application.Common;
using Khyout.Application.Common.Cqrs;
using Microsoft.EntityFrameworkCore;

namespace Khyout.Application.Features.Rfqs;

public sealed record CloseRfqCommand(Guid RfqId) : ICommand<Unit>;

public sealed class CloseRfqCommandValidator : AbstractValidator<CloseRfqCommand>
{
    public CloseRfqCommandValidator()
    {
        RuleFor(c => c.RfqId).NotEmpty();
    }
}

public sealed class CloseRfqCommandHandler(IAppDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    : ICommandHandler<CloseRfqCommand, Unit>
{
    public async Task<Unit> Handle(CloseRfqCommand command, CancellationToken cancellationToken)
    {
        var companyId = currentUser.CompanyId
            ?? throw new ForbiddenException("No company is associated with this account.");

        var rfq = await db.RfqRequests.FirstOrDefaultAsync(r => r.Id == command.RfqId, cancellationToken)
            ?? throw new NotFoundException("RFQ not found.");

        if (rfq.BuyerCompanyId != companyId)
        {
            throw new ForbiddenException("Only the RFQ owner can close it.");
        }

        rfq.Cancel(clock.UtcNow);
        await db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
