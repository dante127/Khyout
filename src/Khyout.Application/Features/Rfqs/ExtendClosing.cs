using FluentValidation;
using Khyout.Application.Abstractions;
using Khyout.Application.Common;
using Khyout.Application.Common.Cqrs;
using Microsoft.EntityFrameworkCore;

namespace Khyout.Application.Features.Rfqs;

public sealed record ExtendClosingCommand(Guid RfqId, DateTimeOffset NewClosingDate) : ICommand<Unit>;

public sealed class ExtendClosingCommandValidator : AbstractValidator<ExtendClosingCommand>
{
    public ExtendClosingCommandValidator()
    {
        RuleFor(c => c.RfqId).NotEmpty();
        RuleFor(c => c.NewClosingDate).NotEmpty();
    }
}

public sealed class ExtendClosingCommandHandler(IAppDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    : ICommandHandler<ExtendClosingCommand, Unit>
{
    public async Task<Unit> Handle(ExtendClosingCommand command, CancellationToken cancellationToken)
    {
        var companyId = currentUser.CompanyId
            ?? throw new ForbiddenException("No company is associated with this account.");

        var rfq = await db.RfqRequests.FirstOrDefaultAsync(r => r.Id == command.RfqId, cancellationToken)
            ?? throw new NotFoundException("RFQ not found.");

        if (rfq.BuyerCompanyId != companyId)
        {
            throw new ForbiddenException("Only the RFQ owner can extend its closing date.");
        }

        rfq.ExtendClosing(command.NewClosingDate, clock.UtcNow);
        await db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
