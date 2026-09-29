using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Khyout.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Khyout.Api.IntegrationTests;

public class TelegramTests : IClassFixture<TestAppFactory>
{
    private readonly TestAppFactory _factory;

    public TelegramTests(TestAppFactory factory)
    {
        _factory = factory;
        _factory.InitializeDatabase();
    }

    [Fact]
    public async Task Link_flow_binds_chat_id_via_webhook()
    {
        var supplier = await TestApi.VerifiedSupplierClientAsync(_factory, "+963900700201", "Telegram Mills", "homs");

        var (linkStatus, linkData, linkError) = await TestApi.SendAsync(supplier, HttpMethod.Post, "/api/v1/telegram/link");
        linkStatus.Should().Be(HttpStatusCode.OK, "{0}", linkError?.ToString());

        var deepLink = linkData!.Value.GetProperty("deepLink").GetString()!;
        deepLink.Should().StartWith("https://t.me/khyout_test_bot?start=");
        var code = deepLink.Split('=')[1];

        var webhookBody = JsonSerializer.Serialize(new
        {
            message = new
            {
                chat = new { id = 777000111 },
                text = $"/start {code}"
            }
        });

        using (var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/telegram/webhook/test-secret")
               {
                   Content = new StringContent(webhookBody, Encoding.UTF8, "application/json")
               })
        {
            var response = await supplier.SendAsync(request);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var chatBound = await _factory.WithDbAsync(async db =>
        {
            var user = await db.Users.FirstAsync(u => u.PhoneNumber == "+963900700201");
            return user.TelegramChatId == "777000111" && user.TelegramLinkCode == null;
        });
        chatBound.Should().BeTrue();
    }

    [Fact]
    public async Task Webhook_with_wrong_secret_returns_not_found()
    {
        var anon = _factory.CreateClient();
        var (status, _, _) = await TestApi.SendAsync(anon, HttpMethod.Post, "/api/v1/telegram/webhook/wrong-secret",
            new { message = new { chat = new { id = 1 }, text = "/start whatever" } });

        status.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Link_requires_authentication()
    {
        var anon = _factory.CreateClient();
        var (status, _, _) = await TestApi.SendAsync(anon, HttpMethod.Post, "/api/v1/telegram/link");
        status.Should().Be(HttpStatusCode.Unauthorized);
    }
}
