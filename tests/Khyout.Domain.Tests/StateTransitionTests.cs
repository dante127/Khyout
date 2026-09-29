using FluentAssertions;
using Khyout.Domain.Common;
using Khyout.Domain.Entities;
using Khyout.Domain.Enums;
using Xunit;

namespace Khyout.Domain.Tests;

public class StateTransitionTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Sample_happy_path_requested_to_received()
    {
        var sample = SampleRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, 2, "damascus", null, Now);

        sample.Status.Should().Be(SampleStatus.Requested);

        sample.Approve(Now.AddHours(1));
        sample.MarkShipped(Now.AddHours(2));
        sample.MarkReceived(Now.AddHours(3));

        sample.Status.Should().Be(SampleStatus.Received);
    }

    [Fact]
    public void Sample_cannot_skip_the_shipping_step()
    {
        var sample = SampleRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, 2, null, null, Now);
        sample.Approve(Now.AddHours(1));

        var act = () => sample.MarkReceived(Now.AddHours(2));

        act.Should().Throw<DomainRuleException>()
            .Which.Code.Should().Be("sample_invalid_transition");
    }

    [Fact]
    public void Rejected_samples_are_terminal()
    {
        var sample = SampleRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, 2, null, null, Now);
        sample.Reject(Now.AddHours(1));

        var act = () => sample.Approve(Now.AddHours(2));

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void Company_verification_only_from_pending()
    {
        var adminId = Guid.NewGuid();
        var company = Company.Create("Atelier Halabi", CompanyType.Supplier, "aleppo", null, null, Now);

        company.VerificationStatus.Should().Be(VerificationStatus.Pending);

        company.Verify(adminId, Now.AddDays(1));

        company.VerificationStatus.Should().Be(VerificationStatus.Verified);
        company.VerifiedByUserId.Should().Be(adminId);
        company.VerifiedAt.Should().Be(Now.AddDays(1));

        var act = () => company.Reject(Now.AddDays(2));
        act.Should().Throw<DomainRuleException>()
            .Which.Code.Should().Be("company_not_pending");
    }

    [Fact]
    public void Buyer_and_supplier_users_require_a_company()
    {
        var act = () => User.Create("+963900000000", "Owner", UserRole.Supplier, null, Now);

        act.Should().Throw<DomainRuleException>()
            .Which.Code.Should().Be("user_company_required");
    }
}
