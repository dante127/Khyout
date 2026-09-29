using Khyout.Domain.Common;
using Khyout.Domain.Enums;

namespace Khyout.Domain.Entities;

/// <summary>A swatch/sample request from a buyer to a supplier.</summary>
public class SampleRequest
{
    private SampleRequest() { } // EF Core

    public Guid Id { get; private set; }
    public Guid BuyerCompanyId { get; private set; }
    public Company BuyerCompany { get; private set; } = null!;
    public Guid SupplierCompanyId { get; private set; }
    public Company SupplierCompany { get; private set; } = null!;
    public Guid ProductId { get; private set; }
    public Product Product { get; private set; } = null!;

    /// <summary>Optional link to the quotation the sample relates to.</summary>
    public Guid? RfqQuotationId { get; private set; }

    public RfqQuotation? RfqQuotation { get; private set; }
    public SampleStatus Status { get; private set; }
    public decimal Quantity { get; private set; }
    public string? DeliveryCity { get; private set; }
    public string? Note { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static SampleRequest Create(
        Guid buyerCompanyId,
        Guid supplierCompanyId,
        Guid productId,
        Guid? rfqQuotationId,
        decimal quantity,
        string? deliveryCity,
        string? note,
        DateTimeOffset now)
    {
        if (quantity <= 0)
        {
            throw new DomainRuleException("sample_quantity_invalid", "Quantity must be greater than zero.");
        }

        return new SampleRequest
        {
            Id = Guid.NewGuid(),
            BuyerCompanyId = buyerCompanyId,
            SupplierCompanyId = supplierCompanyId,
            ProductId = productId,
            RfqQuotationId = rfqQuotationId,
            Status = SampleStatus.Requested,
            Quantity = quantity,
            DeliveryCity = deliveryCity,
            Note = note,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Approve(DateTimeOffset now)
    {
        EnsureStatus(SampleStatus.Requested, "Only requested samples can be approved.");
        Status = SampleStatus.Approved;
        UpdatedAt = now;
    }

    public void Reject(DateTimeOffset now)
    {
        EnsureStatus(SampleStatus.Requested, "Only requested samples can be rejected.");
        Status = SampleStatus.Rejected;
        UpdatedAt = now;
    }

    public void MarkShipped(DateTimeOffset now)
    {
        EnsureStatus(SampleStatus.Approved, "Only approved samples can be shipped.");
        Status = SampleStatus.Shipped;
        UpdatedAt = now;
    }

    public void MarkReceived(DateTimeOffset now)
    {
        EnsureStatus(SampleStatus.Shipped, "Only shipped samples can be received.");
        Status = SampleStatus.Received;
        UpdatedAt = now;
    }

    private void EnsureStatus(SampleStatus expected, string message)
    {
        if (Status != expected)
        {
            throw new DomainRuleException("sample_invalid_transition", message);
        }
    }
}
