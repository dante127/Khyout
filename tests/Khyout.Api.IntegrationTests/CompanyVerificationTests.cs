using System.Net;
using FluentAssertions;
using Xunit;

namespace Khyout.Api.IntegrationTests;

public class CompanyVerificationTests : IClassFixture<TestAppFactory>
{
    private readonly TestAppFactory _factory;

    public CompanyVerificationTests(TestAppFactory factory)
    {
        _factory = factory;
        _factory.InitializeDatabase();
    }

    [Fact]
    public async Task Pending_company_becomes_verified_after_admin_approval()
    {
        var buyer = await TestApi.OnboardAsync(_factory, "+963900200001", "Buyer Owner", "Damascus Stitches", "Buyer", "damascus");
        var buyerClient = _factory.WithToken(buyer.AccessToken);

        var (beforeStatus, beforeData, _) = await TestApi.SendAsync(buyerClient, HttpMethod.Get, "/api/v1/companies/me");
        beforeStatus.Should().Be(HttpStatusCode.OK);
        beforeData!.Value.GetProperty("verificationStatus").GetString().Should().Be("Pending");

        var admin = await TestApi.CreateAdminClientAsync(_factory);
        var companyId = await TestApi.FindCompanyIdByNameAsync(admin, "Damascus Stitches");
        await TestApi.VerifyCompanyAsync(admin, companyId);

        var (afterStatus, afterData, _) = await TestApi.SendAsync(buyerClient, HttpMethod.Get, "/api/v1/companies/me");
        afterStatus.Should().Be(HttpStatusCode.OK);
        afterData!.Value.GetProperty("verificationStatus").GetString().Should().Be("Verified");
    }

    [Fact]
    public async Task Non_admin_cannot_verify_companies()
    {
        var supplier = await TestApi.OnboardAsync(_factory, "+963900200002", "Supplier Two", "Latakia Mills", "Supplier", "latakia");
        var supplierClient = _factory.WithToken(supplier.AccessToken);

        var (status, _, _) = await TestApi.SendAsync(supplierClient, HttpMethod.Post,
            "/api/v1/companies/00000000-0000-0000-0000-000000000001/verify", new { approved = true });

        status.Should().Be(HttpStatusCode.Forbidden);
    }
}
