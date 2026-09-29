using FluentValidation;
using Khyout.Application.Abstractions;
using Khyout.Application.Common;
using Khyout.Application.Common.Cqrs;
using Khyout.Domain.Entities;
using Khyout.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Khyout.Application.Features.Catalog;

public sealed record FiberInput(FiberType FiberType, decimal Percentage);

public sealed record CreateProductCommand(
    Guid CategoryId,
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
    IReadOnlyList<FiberInput> Composition) : ICommand<ProductDetailDto>;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(c => c.CategoryId).NotEmpty();
        RuleFor(c => c.Title).NotEmpty().MaximumLength(300);
        RuleFor(c => c.Description).MaximumLength(4000);
        RuleFor(c => c.Moq).GreaterThan(0);
        RuleFor(c => c.IndicativePrice).GreaterThan(0).When(c => c.IndicativePrice.HasValue);
        RuleFor(c => c.Currency).Length(3).When(c => !string.IsNullOrWhiteSpace(c.Currency));
        RuleFor(c => c.Gsm).GreaterThan(0);
        RuleFor(c => c.GsmTolerancePct).InclusiveBetween(0, 100);
        RuleFor(c => c.WidthCm).GreaterThan(0);
        RuleFor(c => c.Composition).NotEmpty();
        RuleForEach(c => c.Composition).ChildRules(fiber =>
        {
            fiber.RuleFor(f => f.Percentage).GreaterThan(0).LessThanOrEqualTo(100);
        });
    }
}

public sealed class CreateProductCommandHandler(IAppDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    : ICommandHandler<CreateProductCommand, ProductDetailDto>
{
    public async Task<ProductDetailDto> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var companyId = currentUser.CompanyId
            ?? throw new ForbiddenException("No company is associated with this account.");

        var now = clock.UtcNow;

        var product = Product.Create(
            companyId,
            command.CategoryId,
            command.Title,
            command.Description,
            command.Moq,
            command.UnitOfMeasure,
            now);

        product.UpdateDetails(
            command.Title,
            command.Description,
            command.Moq,
            command.UnitOfMeasure,
            command.IndicativePrice,
            command.Currency,
            now);

        product.SetAttributes(
            FabricAttributes.Create(
                product.Id,
                command.Gsm,
                command.GsmTolerancePct,
                command.WeaveStructure,
                command.WidthCm,
                command.ColorFamily,
                command.WeightPerMeterG,
                command.CareNotes),
            now);

        product.ReplaceComposition(command.Composition.Select(f => (f.FiberType, f.Percentage)), now);
        product.EnsureCompositionIsValid();

        db.Products.Add(product);
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
