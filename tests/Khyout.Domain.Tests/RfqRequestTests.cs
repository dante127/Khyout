using FluentAssertions;
using Khyout.Domain.Common;
using Khyout.Domain.Entities;
using Khyout.Domain.Enums;
using Xunit;

namespace Khyout.Domain.Tests;

public class RfqRequestTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);

    private static RfqRequest NewRfq(DateTimeOffset? closing = null) =>
        RfqRequest.Create(
            buyerCompanyId: Guid.NewGuid(),
            categoryId: Guid.NewGuid(),
            title: "Cotton jersey 180 gsm",
            description: null,
            quantityNeeded: 500,
            unitOfMeasure: UnitOfMeasure.Kg,
            targetDeliveryDate: DateOnly.FromDateTime(Now.UtcDateTime).AddDays(30),
            closingDate: closing ?? Now.AddDays(7),
            now: Now);

    [Fact]
    public void Create_rejects_non_positive_quantity()
    {
        var act = () => RfqRequest.Create(Guid.NewGuid(), Guid.NewGuid(), "t", null, 0, UnitOfMeasure.Kg,
            DateOnly.FromDateTime(Now.UtcDateTime).AddDays(30), Now.AddDays(7), Now);

        act.Should().Throw<DomainRuleException>()
            .Which.Code.Should().Be("rfq_quantity_invalid");
    }

    [Fact]
    public void Create_rejects_closing_date_in_the_past()
    {
        var act = () => NewRfq(Now.AddMinutes(-5));

        act.Should().Throw<DomainRuleException>()
            .Which.Code.Should().Be("rfq_closing_invalid");
    }

    [Fact]
    public void Bids_stop_after_closing_date()
    {
        var rfq = NewRfq();

        rfq.CanReceiveBids(Now.AddDays(6)).Should().BeTrue();
        rfq.CanReceiveBids(Now.AddDays(8)).Should().BeFalse();
    }

    [Fact]
    public void Award_sets_the_accepted_quotation_and_status()
    {
        var rfq = NewRfq();
        var quotationId = Guid.NewGuid();

        rfq.Award(quotationId, Now.AddDays(1));

        rfq.Status.Should().Be(RfqStatus.Awarded);
        rfq.AcceptedQuotationId.Should().Be(quotationId);
    }

    [Fact]
    public void Awarded_rfqs_cannot_be_awarded_again()
    {
        var rfq = NewRfq();
        rfq.Award(Guid.NewGuid(), Now.AddDays(1));

        var act = () => rfq.Award(Guid.NewGuid(), Now.AddDays(1));

        act.Should().Throw<DomainRuleException>()
            .Which.Code.Should().Be("rfq_not_open");
    }

    [Fact]
    public void Cancelled_rfqs_stop_receiving_bids()
    {
        var rfq = NewRfq();
        rfq.Cancel(Now.AddDays(1));

        rfq.Status.Should().Be(RfqStatus.Cancelled);
        rfq.CanReceiveBids(Now.AddDays(2)).Should().BeFalse();

        var act = () => rfq.Award(Guid.NewGuid(), Now.AddDays(2));
        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void Expiry_worker_can_expire_open_rfqs_only()
    {
        var rfq = NewRfq();
        rfq.MarkExpired(Now.AddDays(8));

        rfq.Status.Should().Be(RfqStatus.Expired);

        var act = () => rfq.MarkExpired(Now.AddDays(9));
        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void Closing_date_can_only_move_forward_while_open()
    {
        var rfq = NewRfq();

        rfq.ExtendClosing(Now.AddDays(10), Now.AddDays(1));

        rfq.ClosingDate.Should().Be(Now.AddDays(10));

        var act = () => rfq.ExtendClosing(Now.AddDays(9), Now.AddDays(1));
        act.Should().Throw<DomainRuleException>()
            .Which.Code.Should().Be("rfq_extend_invalid");
    }
}
