using Khyout.Domain.Common;
using Khyout.Domain.Enums;

namespace Khyout.Domain.Entities;

/// <summary>One fiber contribution of a product's composition (e.g. 95% Cotton).</summary>
public class FabricComposition
{
    private FabricComposition() { } // EF Core

    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public Product Product { get; private set; } = null!;
    public FiberType FiberType { get; private set; }
    public decimal Percentage { get; private set; }

    public static FabricComposition Create(Guid productId, FiberType fiberType, decimal percentage)
    {
        if (percentage <= 0)
        {
            throw new DomainRuleException("composition_percentage_invalid", "Percentage must be greater than zero.");
        }

        if (percentage > 100)
        {
            throw new DomainRuleException("composition_percentage_invalid", "Percentage cannot exceed 100.");
        }

        return new FabricComposition
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            FiberType = fiberType,
            Percentage = percentage
        };
    }
}
