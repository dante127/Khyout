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

public sealed record CreateSampleRequestCommand(
    Guid ProductId,
    decimal Quantity,
    string? DeliveryCity,
    string? Note,
    Guid? RfqQuotationId) : ICommand<SampleDto>;

public sealed class CreateSampleRequestCommandValidator : AbstractValidator<CreateSampleRequestCommand>
{
    public CreateSampleRequestCommandValidator()
    {
        RuleFor(c => c.ProductId).NotEmpty();
        RuleFor(c => c.Quantity).GreaterThan(0);
        RuleFor(c => c.DeliveryCity).MaximumLength(100);
        RuleFor(c => c.Note).MaximumLength(1000);
    }
}

public sealed class CreateSampleRequestCommandHandler(
    IAppDbContext db,
    ICurrentUser currentUser,
    IDateTimeProvider clock)
    : ICommandHandler<CreateSampleRequestCommand, SampleDto>
{
    public async Task<SampleDto> Handle(CreateSampleRequestCommand command, CancellationToken cancellationToken)
    {
        var companyId = currentUser.CompanyId
            ?? throw new ForbiddenException("No company is associated with this account.");

        var now = clock.UtcNow;

        var product = await db.Products
            .FirstOrDefaultAsync(p => p.Id == command.ProductId, cancellationToken)
            ?? throw new NotFoundException("Product not found.");

        if (product.Status != ProductStatus.Active)
        {
            throw new DomainRuleException("product_not_active", "This product is not available for sampling.");
        }

        if (command.RfqQuotationId is { } quotationId)
        {
            var quotation = await db.RfqQuotations
                .Include(q => q.RfqRequest)
                .FirstOrDefaultAsync(q => q.Id == quotationId, cancellationToken)
                ?? throw new NotFoundException("Quotation not found.");

            if (quotation.SupplierCompanyId != product.SupplierCompanyId ||
                quotation.RfqRequest.BuyerCompanyId != companyId)
            {
                throw new ForbiddenException("The referenced quotation does not belong to this buyer/supplier pair.");
            }
        }

        var sample = SampleRequest.Create(
            companyId,
            product.SupplierCompanyId,
            product.Id,
            command.RfqQuotationId,
            command.Quantity,
            command.DeliveryCity,
            command.Note,
            now);

        db.SampleRequests.Add(sample);

        db.OutboxMessages.Add(OutboxMessage.Create(
            OutboxMessageType.SampleRequested,
            JsonSerializer.Serialize(new
            {
                sampleId = sample.Id,
                supplierCompanyId = product.SupplierCompanyId
            }),
            product.SupplierCompanyId.ToString(),
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
