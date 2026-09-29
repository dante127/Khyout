using Khyout.Domain.Common;
using Khyout.Domain.Enums;

namespace Khyout.Domain.Entities;

/// <summary>Mandatory textile-technical specs, 1:1 with a product.</summary>
public class FabricAttributes
{
    private FabricAttributes() { } // EF Core

    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public Product Product { get; private set; } = null!;

    /// <summary>Fabric weight in grams per square meter.</summary>
    public int Gsm { get; private set; }

    /// <summary>Declared tolerance in percent, e.g. 5 means ±5%.</summary>
    public int GsmTolerancePct { get; private set; }

    public WeaveStructure WeaveStructure { get; private set; }
    public int WidthCm { get; private set; }
    public string? ColorFamily { get; private set; }
    public int? WeightPerMeterG { get; private set; }
    public string? CareNotes { get; private set; }

    public static FabricAttributes Create(
        Guid productId,
        int gsm,
        int gsmTolerancePct,
        WeaveStructure weaveStructure,
        int widthCm,
        string? colorFamily = null,
        int? weightPerMeterG = null,
        string? careNotes = null)
    {
        if (gsm <= 0)
        {
            throw new DomainRuleException("fabric_gsm_invalid", "GSM must be greater than zero.");
        }

        if (gsmTolerancePct < 0 || gsmTolerancePct > 100)
        {
            throw new DomainRuleException("fabric_gsm_tolerance_invalid", "GSM tolerance must be between 0 and 100.");
        }

        if (widthCm <= 0)
        {
            throw new DomainRuleException("fabric_width_invalid", "Width must be greater than zero.");
        }

        if (weightPerMeterG is <= 0)
        {
            throw new DomainRuleException("fabric_weight_invalid", "Weight per linear meter must be greater than zero when provided.");
        }

        return new FabricAttributes
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Gsm = gsm,
            GsmTolerancePct = gsmTolerancePct,
            WeaveStructure = weaveStructure,
            WidthCm = widthCm,
            ColorFamily = colorFamily,
            WeightPerMeterG = weightPerMeterG,
            CareNotes = careNotes
        };
    }
}
