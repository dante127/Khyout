using FluentValidation;
using Khyout.Application.Abstractions;
using Khyout.Application.Common;
using Khyout.Application.Common.Cqrs;
using Khyout.Domain.Entities;
using Khyout.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Khyout.Application.Features.Catalog;

public sealed record UpdateProductCommand(
    Guid ProductId,
    string Title,
    string? Description,
    decimal Moq,
    UnitOfMeasure UnitOfMeasure,
    decimal? IndicativePrice,
    string? Currency,
    int Gsm,
    int GsmTolerancePct,
    WeaveStructure WeaveStructure,
    int WidthCm,
    string? ColorFamily,
    int? WeightPerMeterG,
    string? CareNotes,
    IReadOnlyList<FiberInput> Composition,
    ProductStatus Status) : ICommand<ProductDetailDto>;

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(c => c.ProductId).NotEmpty();
        RuleFor(c => c.Title).NotEmpty().MaximumLength(300);
        RuleFor(c => c.Description).MaximumLength(4000);
        RuleFor(c => c.Moq).GreaterThan(0);
        RuleFor(c => c.IndicativePrice).GreaterThan(0).When(c => c.IndicativePrice.HasValue);
        RuleFor(c => c.Currency).Length(3).When(c => !string.IsNullOrWhiteSpace(c.Currency));
        RuleFor(c => c.Gsm).GreaterThan(0);
        RuleFor(c => c.GsmTolerancePct).InclusiveBetween(0, 100);
        RuleFor(c => c.WidthCm).GreaterThan(0);
        RuleFor(c => c.Composition).NotEmpty();
        RuleFor(c => c.Status).IsInEnum();
        RuleForEach(c => c.Composition).ChildRules(fiber =>
        {
            fiber.RuleFor(f => f.Percentage).GreaterThan(0).LessThanOrEqualTo(100);
        });
    }
}

public sealed class UpdateProductCommandHandler(IAppDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    : ICommandHandler<UpdateProductCommand, ProductDetailDto>
{
    public async Task<ProductDetailDto> Handle(UpdateProductCommand command, CancellationToken cancellationToken)
    {
        var companyId = currentUser.CompanyId
            ?? throw new ForbiddenException("No company is associated with this account.");

        var now = clock.UtcNow;

        var product = await db.Products
            .Include(p => p.Attributes)
            .Include(p => p.Composition)
            .FirstOrDefaultAsync(p => p.Id == command.ProductId, cancellationToken)
            ?? throw new NotFoundException("Product not found.");

        if (product.SupplierCompanyId != companyId)
        {
            throw new ForbiddenException("Only the product owner can update it.");
        }

        product.UpdateDetails(
            command.Title,
            command.Description,
            command.Moq,
            command.UnitOfMeasure,
            command.IndicativePrice,
            command.Currency,
            now);

        if (product.Attributes is null)
        {
            var attributes = FabricAttributes.Create(
                product.Id,
                command.Gsm,
                command.GsmTolerancePct,
                command.WeaveStructure,
                command.WidthCm,
                command.ColorFamily,
                command.WeightPerMeterG,
                command.CareNotes);
            product.SetAttributes(attributes, now);

            // Explicit insert: new entities with pre-assigned keys that EF discovers
            // through navigations would otherwise be treated as updates.
            db.FabricAttributes.Add(attributes);
        }
        else
        {
            product.Attributes.Update(
                command.Gsm,
                command.GsmTolerancePct,
                command.WeaveStructure,
                command.WidthCm,
                command.ColorFamily,
                command.WeightPerMeterG,
                command.CareNotes);
        }

        product.ReplaceComposition(command.Composition.Select(f => (f.FiberType, f.Percentage)), now);

        // ReplaceComposition cleared the tracked collection (old rows become deletes) and
        // created fresh rows — register those explicitly as inserts.
        db.FabricCompositions.AddRange(product.Composition);

        product.ChangeStatus(command.Status, now);

        await db.SaveChangesAsync(cancellationToken);

        var saved = await db.Products
            .Include(p => p.Attributes)
            .Include(p => p.Composition)
            .Include(p => p.Images)
            .Include(p => p.SupplierCompany)
            .Include(p => p.Category)
            .FirstAsync(p => p.Id == product.Id, cancellationToken);

        return ProductDetailDto.From(saved);
    }
}
