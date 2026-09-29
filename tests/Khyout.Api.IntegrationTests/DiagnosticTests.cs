using System.Net;
using FluentAssertions;
using Xunit;

namespace Khyout.Api.IntegrationTests;

/// <summary>Smoke test for the most basic authenticated flow; failure output includes the server error detail.</summary>
public class DiagnosticTests : IClassFixture<TestAppFactory>
{
    private readonly TestAppFactory _factory;

    public DiagnosticTests(TestAppFactory factory)
    {
        _factory = factory;
        _factory.InitializeDatabase();
    }

    [Fact]
    public async Task Fresh_buyer_can_fetch_own_company()
    {
        var buyer = await TestApi.OnboardAsync(_factory, "+963900800001", "Diag Buyer", "Diag Buyers", "Buyer", "damascus");
        var client = _factory.WithToken(buyer.AccessToken);

        var (status, data, error) = await TestApi.SendAsync(client, HttpMethod.Get, "/api/v1/companies/me");
        status.Should().Be(
            HttpStatusCode.OK,
            "error: {0} | data: {1}",
            error?.ToString() ?? "(none)",
            data?.ToString() ?? "(none)");
    }
}
