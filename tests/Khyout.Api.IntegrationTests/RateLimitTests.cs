using System.Net;
using FluentAssertions;
using Khyout.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Khyout.Api.IntegrationTests;

/// <summary>Dedicated factory with a low OTP rate limit to prove throttling works.</summary>
public sealed class LimitedRateFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"khyout-rl-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:UseSqlite"] = "true",
                ["ConnectionStrings:Sqlite"] = $"Data Source={_dbPath}",
                ["BackgroundJobs:Enabled"] = "false",
                ["RateLimiting:OtpPermitLimit"] = "3",
                ["Auth:Jwt:Issuer"] = "khyout-tests",
                ["Auth:Jwt:Audience"] = "khyout-tests",
                ["Auth:Jwt:SigningKey"] = "test-signing-key-0123456789-0123456789-0123456789"
            });
        });
    }

    public void InitializeDatabase()
    {
        using var scope = Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
    }
}

public class RateLimitTests : IClassFixture<LimitedRateFactory>
{
    private readonly LimitedRateFactory _factory;

    public RateLimitTests(LimitedRateFactory factory)
    {
        _factory = factory;
        _factory.InitializeDatabase();
    }

    [Fact]
    public async Task Otp_requests_are_rate_limited()
    {
        var client = _factory.CreateClient();

        for (var i = 1; i <= 3; i++)
        {
            var (status, _, _) = await TestApi.SendAsync(client, HttpMethod.Post, "/api/v1/auth/otp/request",
                new { phoneNumber = $"+96390070000{i}", purpose = "Login" });
            status.Should().Be(HttpStatusCode.OK);
        }

        var (limited, _, _) = await TestApi.SendAsync(client, HttpMethod.Post, "/api/v1/auth/otp/request",
            new { phoneNumber = "+963900700004", purpose = "Login" });

        limited.Should().Be(HttpStatusCode.TooManyRequests);
    }
}
