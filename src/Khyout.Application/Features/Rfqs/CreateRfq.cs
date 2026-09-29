using System.Text.Json;
using FluentValidation;
using Khyout.Application.Abstractions;
using Khyout.Application.Common;
using Khyout.Application.Common.Cqrs;
using Khyout.Domain.Entities;
using Khyout.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Khyout.Application.Features.Rfqs;

public sealed record CreateRfqCommand(
    Guid CategoryId,
    string Title,
    string? Description,
    decimal QuantityNeeded,
    UnitOfMeasure UnitOfMeasure,
    DateOnly TargetDeliveryDate,
    DateTimeOffset ClosingDate) : ICommand<RfqDetailDto>;

public sealed class CreateRfqCommandValidator : AbstractValidator<CreateRfqCommand>
{
    public CreateRfqCommandValidator()
    {
        RuleFor(c => c.CategoryId).NotEmpty();
        RuleFor(c => c.Title).NotEmpty().MaximumLength(300);
        RuleFor(c => c.Description).MaximumLength(4000);
        RuleFor(c => c.QuantityNeeded).GreaterThan(0);
    }
}

public sealed class CreateRfqCommandHandler(IAppDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    : ICommandHandler<CreateRfqCommand, RfqDetailDto>
{
    public async Task<RfqDetailDto> Handle(CreateRfqCommand command, CancellationToken cancellationToken)
    {
        var companyId = currentUser.CompanyId
            ?? throw new ForbiddenException("No company is associated with this account.");

        var now = clock.UtcNow;

        var rfq = RfqRequest.Create(
            companyId,
            command.CategoryId,
            command.Title,
            command.Description,
            command.QuantityNeeded,
            command.UnitOfMeasure,
            command.TargetDeliveryDate,
            command.ClosingDate,
            now);

        db.RfqRequests.Add(rfq);

        // Notify interested suppliers (recipient resolution happens in the Phase 4 dispatcher).
        db.OutboxMessages.Add(OutboxMessage.Create(
            OutboxMessageType.RfqCreated,
            JsonSerializer.Serialize(new { rfqId = rfq.Id, categoryId = rfq.CategoryId, title = rfq.Title }),
            rfq.CategoryId.ToString(),
            now));

        await db.SaveChangesAsync(cancellationToken);

        var detail = await db.RfqRequests
            .Include(r => r.Category)
            .Include(r => r.BuyerCompany)
            .FirstAsync(r => r.Id == rfq.Id, cancellationToken);

        return RfqMapping.ToDetail(detail);
    }
}
