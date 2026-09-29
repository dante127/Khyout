using Khyout.Domain.Common;
using Khyout.Domain.Enums;

namespace Khyout.Domain.Entities;

/// <summary>
/// A supplier's time-bound bid on an RFQ. <see cref="ValidUntil"/> is mandatory and
/// must be in the future at submission; expired bids can never be accepted.
/// </summary>
public class RfqQuotation
{
    private RfqQuotation() { } // EF Core

    public Guid Id { get; private set; }
    public Guid RfqRequestId { get; private set; }
    public RfqRequest RfqRequest { get; private set; } = null!;
    public Guid SupplierCompanyId { get; private set; }
    public Company SupplierCompany { get; private set; } = null!;

    /// <summary>Hidden from competing suppliers (projection-level rule).</summary>
    public decimal UnitPrice { get; private set; }

    public string Currency { get; private set; } = null!;
    public DateTimeOffset ValidUntil { get; private set; }
    public int LeadTimeDays { get; private set; }
    public string? Note { get; private set; }
    public QuotationStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static RfqQuotation Create(
        Guid rfqRequestId,
        Guid supplierCompanyId,
        decimal unitPrice,
        string currency,
        DateTimeOffset validUntil,
        int leadTimeDays,
        string? note,
        DateTimeOffset now)
    {
        if (unitPrice <= 0)
        {
            throw new DomainRuleException("quotation_price_invalid", "Unit price must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new DomainRuleException("quotation_currency_required", "Currency is required.");
        }

        if (validUntil <= now)
        {
            throw new DomainRuleException("quotation_validity_invalid", "ValidUntil must be in the future.");
        }

        if (leadTimeDays < 0)
        {
            throw new DomainRuleException("quotation_lead_time_invalid", "Lead time cannot be negative.");
        }

        return new RfqQuotation
        {
            Id = Guid.NewGuid(),
            RfqRequestId = rfqRequestId,
            SupplierCompanyId = supplierCompanyId,
            UnitPrice = unitPrice,
            Currency = currency.Trim().ToUpperInvariant(),
            ValidUntil = validUntil,
            LeadTimeDays = leadTimeDays,
            Note = note,
            Status = QuotationStatus.Submitted,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public bool IsExpired(DateTimeOffset now) => ValidUntil <= now;

    /// <summary>Guards the accept path: only Submitted, non-expired quotations can be accepted.</summary>
    public void EnsureAcceptable(DateTimeOffset now)
    {
        if (Status != QuotationStatus.Submitted)
        {
            throw new DomainRuleException("quotation_not_submitted", "Only submitted quotations can be accepted.");
        }

        if (IsExpired(now))
        {
            throw new DomainRuleException("quotation_expired", "This quotation has expired and can no longer be accepted.");
        }
    }

    public void MarkAccepted(DateTimeOffset now)
    {
        EnsureAcceptable(now);
        Status = QuotationStatus.Accepted;
        UpdatedAt = now;
    }

    public void MarkRejected(DateTimeOffset now)
    {
        EnsureSubmitted();
        Status = QuotationStatus.Rejected;
        UpdatedAt = now;
    }

    public void MarkExpired(DateTimeOffset now)
    {
        EnsureSubmitted();
        Status = QuotationStatus.Expired;
        UpdatedAt = now;
    }

    public void Withdraw(DateTimeOffset now)
    {
        EnsureSubmitted();
        Status = QuotationStatus.Withdrawn;
        UpdatedAt = now;
    }

    private void EnsureSubmitted()
    {
        if (Status != QuotationStatus.Submitted)
        {
            throw new DomainRuleException("quotation_not_submitted", "Only submitted quotations can change state.");
        }
    }
}
