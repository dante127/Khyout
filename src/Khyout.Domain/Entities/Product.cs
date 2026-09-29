using Khyout.Domain.Common;
using Khyout.Domain.Enums;

namespace Khyout.Domain.Entities;

/// <summary>Supplier listing for a fabric or yarn product.</summary>
public class Product
{
    /// <summary>Allowed absolute deviation when checking that fiber percentages sum to 100.</summary>
    public const decimal CompositionSumTolerance = 0.5m;

    private Product() { } // EF Core

    public Guid Id { get; private set; }
    public Guid SupplierCompanyId { get; private set; }
    public Company SupplierCompany { get; private set; } = null!;
    public Guid CategoryId { get; private set; }
    public Category Category { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public decimal Moq { get; private set; }
    public UnitOfMeasure UnitOfMeasure { get; private set; }

    /// <summary>Informational only — RFQ quotations are the binding prices.</summary>
    public decimal? IndicativePrice { get; private set; }

    public string? Currency { get; private set; }
    public ProductStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public FabricAttributes? Attributes { get; private set; }
    public ICollection<FabricComposition> Composition { get; private set; } = new List<FabricComposition>();
    public ICollection<ProductImage> Images { get; private set; } = new List<ProductImage>();

    public static Product Create(
        Guid supplierCompanyId,
        Guid categoryId,
        string title,
        string? description,
        decimal moq,
        UnitOfMeasure unitOfMeasure,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainRuleException("product_title_required", "Product title is required.");
        }

        if (moq <= 0)
        {
            throw new DomainRuleException("product_moq_invalid", "Minimum order quantity must be greater than zero.");
        }

        return new Product
        {
            Id = Guid.NewGuid(),
            SupplierCompanyId = supplierCompanyId,
            CategoryId = categoryId,
            Title = title.Trim(),
            Description = description,
            Moq = moq,
            UnitOfMeasure = unitOfMeasure,
            Status = ProductStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public FabricComposition AddComposition(FiberType fiberType, decimal percentage)
    {
        if (Composition.Any(c => c.FiberType == fiberType))
        {
            throw new DomainRuleException("composition_duplicate_fiber", $"Fiber '{fiberType}' already exists on this product.");
        }

        var item = FabricComposition.Create(Id, fiberType, percentage);
        Composition.Add(item);
        return item;
    }

    /// <summary>Enforces the fabric composition invariant: percentages sum to 100 (±0.5) with at least one row.</summary>
    public void EnsureCompositionIsValid()
    {
        if (Composition.Count == 0)
        {
            throw new DomainRuleException("composition_required", "At least one fiber composition row is required.");
        }

        var sum = Composition.Sum(c => c.Percentage);
        if (Math.Abs(sum - 100m) > CompositionSumTolerance)
        {
            throw new DomainRuleException(
                "composition_sum_invalid",
                $"Fiber percentages must sum to 100 (±{CompositionSumTolerance}); they sum to {sum}.");
        }
    }

    public bool HasValidComposition()
    {
        if (Composition.Count == 0)
        {
            return false;
        }

        return Math.Abs(Composition.Sum(c => c.Percentage) - 100m) <= CompositionSumTolerance;
    }

    public void UpdateDetails(
        string title,
        string? description,
        decimal moq,
        UnitOfMeasure unitOfMeasure,
        decimal? indicativePrice,
        string? currency,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainRuleException("product_title_required", "Product title is required.");
        }

        if (moq <= 0)
        {
            throw new DomainRuleException("product_moq_invalid", "Minimum order quantity must be greater than zero.");
        }

        if (indicativePrice is <= 0)
        {
            throw new DomainRuleException("product_price_invalid", "Indicative price must be greater than zero when provided.");
        }

        if (indicativePrice is not null && string.IsNullOrWhiteSpace(currency))
        {
            throw new DomainRuleException("product_currency_required", "Currency is required when a price is provided.");
        }

        Title = title.Trim();
        Description = description;
        Moq = moq;
        UnitOfMeasure = unitOfMeasure;
        IndicativePrice = indicativePrice;
        Currency = currency?.Trim().ToUpperInvariant();
        UpdatedAt = now;
    }

    public void SetAttributes(FabricAttributes attributes, DateTimeOffset now)
    {
        Attributes = attributes;
        UpdatedAt = now;
    }

    public void ReplaceComposition(IEnumerable<(FiberType Fiber, decimal Percentage)> fibers, DateTimeOffset now)
    {
        Composition.Clear();
        foreach (var (fiber, percentage) in fibers)
        {
            Composition.Add(FabricComposition.Create(Id, fiber, percentage));
        }

        UpdatedAt = now;
    }

    public void ChangeStatus(ProductStatus status, DateTimeOffset now)
    {
        if (status == ProductStatus.Active)
        {
            if (Attributes is null)
            {
                throw new DomainRuleException("product_attributes_required", "Technical attributes are required before publishing.");
            }

            EnsureCompositionIsValid();
        }

        Status = status;
        UpdatedAt = now;
    }
}
