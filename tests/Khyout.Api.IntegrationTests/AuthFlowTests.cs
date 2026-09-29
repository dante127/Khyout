using System.Net;
using FluentAssertions;
using Xunit;

namespace Khyout.Api.IntegrationTests;

public class AuthFlowTests : IClassFixture<TestAppFactory>
{
    private readonly TestAppFactory _factory;

    public AuthFlowTests(TestAppFactory factory)
    {
        _factory = factory;
        _factory.InitializeDatabase();
    }

    [Fact]
    public async Task Onboarding_then_login_then_refresh_rotation_works()
    {
        var phone = "+963900100001";
        var tokens = await TestApi.OnboardAsync(_factory, phone, "Owner One", "Rfq Textiles", "Supplier", "aleppo");

        tokens.AccessToken.Should().NotBeNullOrWhiteSpace();
        tokens.RefreshToken.Should().NotBeNullOrWhiteSpace();

        var login = await TestApi.LoginAsync(_factory, phone);
        login.AccessToken.Should().NotBeNullOrWhiteSpace();

        // Refresh rotates: the old token must stop working, the new one must work.
        var anon = _factory.CreateClient();
        var (status1, data1, _) = await TestApi.SendAsync(anon, HttpMethod.Post, "/api/v1/auth/refresh",
            new { refreshToken = login.RefreshToken });
        status1.Should().Be(HttpStatusCode.OK);
        var rotated = data1!.Value.GetProperty("refreshToken").GetString()!;
        rotated.Should().NotBe(login.RefreshToken);

        var (status2, _, error2) = await TestApi.SendAsync(anon, HttpMethod.Post, "/api/v1/auth/refresh",
            new { refreshToken = login.RefreshToken });
        status2.Should().Be(HttpStatusCode.Conflict);
        error2!.Value.GetProperty("code").GetString().Should().Be("refresh_expired");

        var (status3, _, _) = await TestApi.SendAsync(anon, HttpMethod.Post, "/api/v1/auth/refresh",
            new { refreshToken = rotated });
        status3.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Wrong_otp_is_rejected_and_attempts_are_limited()
    {
        var phone = "+963900100002";
        await TestApi.RequestOtpAsync(_factory.CreateClient(), phone, "Login");

        var anon = _factory.CreateClient();

        for (var attempt = 1; attempt <= 5; attempt++)
        {
            var (status, _, error) = await TestApi.SendAsync(anon, HttpMethod.Post, "/api/v1/auth/otp/verify",
                new { phoneNumber = phone, code = "000000" });
            status.Should().Be(HttpStatusCode.Conflict);
            error!.Value.GetProperty("code").GetString().Should().Be("otp_invalid");
        }

        // Sixth attempt: the code is locked after MaxAttempts failed tries.
        var (finalStatus, _, finalError) = await TestApi.SendAsync(anon, HttpMethod.Post, "/api/v1/auth/otp/verify",
            new { phoneNumber = phone, code = "000000" });
        finalStatus.Should().Be(HttpStatusCode.Conflict);
        finalError!.Value.GetProperty("code").GetString().Should().Be("otp_attempts_exceeded");
    }

    [Fact]
    public async Task Login_for_unknown_phone_reports_user_not_found()
    {
        var phone = "+963900100003";
        await TestApi.RequestOtpAsync(_factory.CreateClient(), phone, "Login");
        var code = _factory.Sms.LatestCodeFor(phone);

        var (status, _, error) = await TestApi.SendAsync(_factory.CreateClient(), HttpMethod.Post,
            "/api/v1/auth/otp/verify", new { phoneNumber = phone, code });

        status.Should().Be(HttpStatusCode.Conflict);
        error!.Value.GetProperty("code").GetString().Should().Be("user_not_found");
    }

    [Fact]
    public async Task Protected_endpoint_requires_token()
    {
        var anon = _factory.CreateClient();
        var (status, _, _) = await TestApi.SendAsync(anon, HttpMethod.Get, "/api/v1/companies/me");
        status.Should().Be(HttpStatusCode.Unauthorized);
    }
}
