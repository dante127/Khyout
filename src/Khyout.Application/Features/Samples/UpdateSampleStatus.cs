using System.Text.Json;
using FluentValidation;
using Khyout.Application.Abstractions;
using Khyout.Application.Common;
using Khyout.Application.Common.Cqrs;
using Khyout.Domain.Common;
using Khyout.Domain.Entities;
using Khyout.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Khyout.Application.Features.Samples;

public sealed record UpdateSampleStatusCommand(Guid SampleId, SampleStatus Status) : ICommand<SampleDto>;

public sealed class UpdateSampleStatusCommandValidator : AbstractValidator<UpdateSampleStatusCommand>
{
    public UpdateSampleStatusCommandValidator()
    {
        RuleFor(c => c.SampleId).NotEmpty();
        RuleFor(c => c.Status).IsInEnum();
    }
}

public sealed class UpdateSampleStatusCommandHandler(
    IAppDbContext db,
    ICurrentUser currentUser,
    IDateTimeProvider clock)
    : ICommandHandler<UpdateSampleStatusCommand, SampleDto>
{
    public async Task<SampleDto> Handle(UpdateSampleStatusCommand command, CancellationToken cancellationToken)
    {
        var companyId = currentUser.CompanyId
            ?? throw new ForbiddenException("No company is associated with this account.");

        var sample = await db.SampleRequests
            .FirstOrDefaultAsync(s => s.Id == command.SampleId, cancellationToken)
            ?? throw new NotFoundException("Sample request not found.");

        var isAdmin = currentUser.Role == UserRole.Admin;
        if (!isAdmin && sample.SupplierCompanyId != companyId)
        {
            throw new ForbiddenException("Only the supplier (or an admin) can update this sample request.");
        }

        var now = clock.UtcNow;

        switch (command.Status)
        {
            case SampleStatus.Approved:
                sample.Approve(now);
                break;
            case SampleStatus.Rejected:
                sample.Reject(now);
                break;
            case SampleStatus.Shipped:
                sample.MarkShipped(now);
                break;
            case SampleStatus.Received:
                sample.MarkReceived(now);
                break;
            default:
                throw new DomainRuleException("sample_status_invalid", "Unsupported target status.");
        }

        db.OutboxMessages.Add(OutboxMessage.Create(
            OutboxMessageType.SampleStatusChanged,
            JsonSerializer.Serialize(new
            {
                sampleId = sample.Id,
                buyerCompanyId = sample.BuyerCompanyId,
                status = sample.Status.ToString()
            }),
            sample.BuyerCompanyId.ToString(),
            now));

        await db.SaveChangesAsync(cancellationToken);

        var saved = await db.SampleRequests
            .Include(s => s.BuyerCompany)
            .Include(s => s.SupplierCompany)
            .Include(s => s.Product)
            .FirstAsync(s => s.Id == sample.Id, cancellationToken);

        return SampleDto.From(saved);
    }
}
