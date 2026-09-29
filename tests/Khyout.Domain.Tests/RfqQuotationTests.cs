using FluentAssertions;
using Khyout.Domain.Common;
using Khyout.Domain.Entities;
using Khyout.Domain.Enums;
using Xunit;

namespace Khyout.Domain.Tests;

public class RfqQuotationTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);

    private static RfqQuotation NewQuotation(DateTimeOffset? validUntil = null) =>
        RfqQuotation.Create(
            rfqRequestId: Guid.NewGuid(),
            supplierCompanyId: Guid.NewGuid(),
            unitPrice: 4.75m,
            currency: "USD",
            validUntil: validUntil ?? Now.AddDays(3),
            leadTimeDays: 10,
            note: null,
            now: Now);

    [Fact]
    public void Create_rejects_non_positive_price()
    {
        var act = () => RfqQuotation.Create(Guid.NewGuid(), Guid.NewGuid(), 0m, "USD", Now.AddDays(1), 5, null, Now);

        act.Should().Throw<DomainRuleException>()
            .Which.Code.Should().Be("quotation_price_invalid");
    }

    [Fact]
    public void Create_rejects_validity_in_the_past()
    {
        var act = () => NewQuotation(Now.AddSeconds(-1));

        act.Should().Throw<DomainRuleException>()
            .Which.Code.Should().Be("quotation_validity_invalid");
    }

    [Fact]
    public void Create_normalizes_currency_and_status()
    {
        var quotation = RfqQuotation.Create(Guid.NewGuid(), Guid.NewGuid(), 4.75m, " usd ", Now.AddDays(3), 10, null, Now);

        quotation.Currency.Should().Be("USD");
        quotation.Status.Should().Be(QuotationStatus.Submitted);
    }

    [Fact]
    public void IsExpired_is_true_at_the_valid_until_instant()
    {
        var quotation = NewQuotation(Now.AddDays(1));

        quotation.IsExpired(Now.AddDays(1)).Should().BeTrue();
        quotation.IsExpired(Now.AddDays(1).AddSeconds(-1)).Should().BeFalse();
    }

    [Fact]
    public void Expired_quotation_cannot_be_accepted()
    {
        var quotation = NewQuotation(Now.AddDays(1));

        var act = () => quotation.MarkAccepted(Now.AddDays(2));

        act.Should().Throw<DomainRuleException>()
            .Which.Code.Should().Be("quotation_expired");
    }

    [Fact]
    public void Submitted_quotation_can_be_accepted_before_expiry()
    {
        var quotation = NewQuotation(Now.AddDays(3));

        quotation.MarkAccepted(Now.AddDays(1));

        quotation.Status.Should().Be(QuotationStatus.Accepted);
    }

    [Fact]
    public void Withdrawn_quotation_cannot_be_accepted()
    {
        var quotation = NewQuotation(Now.AddDays(3));
        quotation.Withdraw(Now);

        var act = () => quotation.MarkAccepted(Now.AddHours(1));

        act.Should().Throw<DomainRuleException>()
            .Which.Code.Should().Be("quotation_not_submitted");
    }

    [Fact]
    public void Expiry_worker_marks_overdue_bid_expired()
    {
        var quotation = NewQuotation(Now.AddHours(5));

        quotation.MarkExpired(Now.AddHours(6));

        quotation.Status.Should().Be(QuotationStatus.Expired);
    }
}
