using Khyout.Domain.Common;
using Khyout.Domain.Enums;

namespace Khyout.Domain.Entities;

/// <summary>A buyer's request for quotation (time-boxed).</summary>
public class RfqRequest
{
    private RfqRequest() { } // EF Core

    public Guid Id { get; private set; }
    public Guid BuyerCompanyId { get; private set; }
    public Company BuyerCompany { get; private set; } = null!;
    public Guid CategoryId { get; private set; }
    public Category Category { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public decimal QuantityNeeded { get; private set; }
    public UnitOfMeasure UnitOfMeasure { get; private set; }
    public DateOnly TargetDeliveryDate { get; private set; }

    /// <summary>No new bids are accepted after this moment.</summary>
    public DateTimeOffset ClosingDate { get; private set; }

    public RfqStatus Status { get; private set; }

    /// <summary>Set when the RFQ is awarded.</summary>
    public Guid? AcceptedQuotationId { get; private set; }

    public RfqQuotation? AcceptedQuotation { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public ICollection<RfqQuotation> Quotations { get; private set; } = new List<RfqQuotation>();

    public static RfqRequest Create(
        Guid buyerCompanyId,
        Guid categoryId,
        string title,
        string? description,
        decimal quantityNeeded,
        UnitOfMeasure unitOfMeasure,
        DateOnly targetDeliveryDate,
        DateTimeOffset closingDate,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainRuleException("rfq_title_required", "Title is required.");
        }

        if (quantityNeeded <= 0)
        {
            throw new DomainRuleException("rfq_quantity_invalid", "Quantity must be greater than zero.");
        }

        if (closingDate <= now)
        {
            throw new DomainRuleException("rfq_closing_invalid", "Closing date must be in the future.");
        }

        if (targetDeliveryDate < DateOnly.FromDateTime(now.UtcDateTime))
        {
            throw new DomainRuleException("rfq_delivery_date_invalid", "Target delivery date cannot be in the past.");
        }

        return new RfqRequest
        {
            Id = Guid.NewGuid(),
            BuyerCompanyId = buyerCompanyId,
            CategoryId = categoryId,
            Title = title.Trim(),
            Description = description,
            QuantityNeeded = quantityNeeded,
            UnitOfMeasure = unitOfMeasure,
            TargetDeliveryDate = targetDeliveryDate,
            ClosingDate = closingDate,
            Status = RfqStatus.Open,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>Bids are only accepted while the RFQ is Open and before ClosingDate.</summary>
    public bool CanReceiveBids(DateTimeOffset now) => Status == RfqStatus.Open && ClosingDate > now;

    public void ExtendClosing(DateTimeOffset newClosingDate, DateTimeOffset now)
    {
        EnsureOpen();
        if (newClosingDate <= ClosingDate)
        {
            throw new DomainRuleException("rfq_extend_invalid", "New closing date must be later than the current one.");
        }

        ClosingDate = newClosingDate;
        UpdatedAt = now;
    }

    public void Award(Guid quotationId, DateTimeOffset now)
    {
        EnsureOpen();
        AcceptedQuotationId = quotationId;
        Status = RfqStatus.Awarded;
        UpdatedAt = now;
    }

    public void Cancel(DateTimeOffset now)
    {
        EnsureOpen();
        Status = RfqStatus.Cancelled;
        UpdatedAt = now;
    }

    public void MarkExpired(DateTimeOffset now)
    {
        EnsureOpen();
        Status = RfqStatus.Expired;
        UpdatedAt = now;
    }

    private void EnsureOpen()
    {
        if (Status != RfqStatus.Open)
        {
            throw new DomainRuleException("rfq_not_open", "Only Open RFQs can change state.");
        }
    }
}
