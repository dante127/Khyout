using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Khyout.Application.Abstractions;
using Khyout.Domain.Entities;
using Khyout.Domain.Enums;
using Khyout.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Khyout.Api.IntegrationTests;

public sealed class TestClock : IDateTimeProvider
{
    public DateTimeOffset UtcNow { get; private set; } = new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);

    public void Advance(TimeSpan by) => UtcNow = UtcNow.Add(by);
}

public sealed class FakeSmsSender : ISmsSender
{
    private readonly List<(string Phone, string Message)> _sent = new();

    public Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        lock (_sent)
        {
            _sent.Add((phoneNumber, message));
        }

        return Task.CompletedTask;
    }

    public string LatestCodeFor(string phoneNumber)
    {
        lock (_sent)
        {
            var latest = _sent.LastOrDefault(s => s.Phone == phoneNumber);
            var match = System.Text.RegularExpressions.Regex.Match(latest.Message ?? string.Empty, @"\b(\d{6})\b");
            match.Success.Should().BeTrue("an OTP SMS should have been captured for " + phoneNumber);
            return match.Groups[1].Value;
        }
    }
}

public sealed class FakeTelegramSender : ITelegramSender
{
    private readonly List<(string Target, string Text)> _sent = new();

    /// <summary>When true, all sends report failure (used for the outbox retry path).</summary>
    public bool FailAll { get; set; }

    public IReadOnlyList<(string Target, string Text)> Sent
    {
        get
        {
            lock (_sent)
            {
                return _sent.ToList();
            }
        }
    }

    public Task<bool> SendAsync(string targetRef, string text, CancellationToken cancellationToken = default)
    {
        lock (_sent)
        {
            _sent.Add((targetRef, text));
        }

        return Task.FromResult(!FailAll);
    }
}

public sealed class TestAppFactory : WebApplicationFactory<Program>
{
    public FakeSmsSender Sms { get; } = new();
    public FakeTelegramSender Telegram { get; } = new();
    public TestClock Clock { get; } = new();

    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"khyout-it-{Guid.NewGuid():N}.db");

    /// <summary>Per-factory media storage root (image pipeline writes here).</summary>
    public string MediaRoot { get; } = Path.Combine(Path.GetTempPath(), $"khyout-media-{Guid.NewGuid():N}");

    private bool _initialized;

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
                ["RateLimiting:OtpPermitLimit"] = "1000",
                ["Storage:MediaRoot"] = MediaRoot,
                ["Telegram:BotUsername"] = "khyout_test_bot",
                ["Telegram:WebhookSecret"] = "test-secret",
                ["RateLimiting:OtpPermitLimit"] = "1000",
                ["Auth:Jwt:Issuer"] = "khyout-tests",
                ["Auth:Jwt:Audience"] = "khyout-tests",
                ["Auth:Jwt:SigningKey"] = "test-signing-key-0123456789-0123456789-0123456789"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ISmsSender>();
            services.AddSingleton<ISmsSender>(Sms);
            services.RemoveAll<ITelegramSender>();
            services.AddSingleton<ITelegramSender>(Telegram);
            services.RemoveAll<IDateTimeProvider>();
            services.AddSingleton<IDateTimeProvider>(Clock);
        });
    }

    public void InitializeDatabase()
    {
        if (_initialized)
        {
            return;
        }

        using var scope = Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
        _initialized = true;
    }

    public async Task<T> WithDbAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        InitializeDatabase();
        using var scope = Services.CreateScope();
        return await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }
}

public static class TestApi
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static async Task<(HttpStatusCode Status, JsonElement? Data, JsonElement? Error)> SendAsync(
        HttpClient client,
        HttpMethod method,
        string url,
        object? body = null)
    {
        using var request = new HttpRequestMessage(method, url);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: Json);
        }

        var response = await client.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text);
        var root = document.RootElement;

        var data = root.TryGetProperty("data", out var d) && d.ValueKind != JsonValueKind.Null
            ? d.Clone()
            : (JsonElement?)null;
        var error = root.TryGetProperty("error", out var e) && e.ValueKind != JsonValueKind.Null
            ? e.Clone()
            : (JsonElement?)null;

        return (response.StatusCode, data, error);
    }

    public static HttpClient WithToken(this WebApplicationFactory<Program> factory, string accessToken)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    public static async Task RequestOtpAsync(HttpClient client, string phone, string purpose)
    {
        var (status, _, _) = await SendAsync(client, HttpMethod.Post, "/api/v1/auth/otp/request",
            new { phoneNumber = phone, purpose });
        status.Should().Be(HttpStatusCode.OK);
    }

    public static async Task<TokenResult> OnboardAsync(
        TestAppFactory factory,
        string phone,
        string fullName,
        string companyName,
        string companyType,
        string city)
    {
        var anon = factory.CreateClient();
        await RequestOtpAsync(anon, phone, "Onboarding");
        var code = factory.Sms.LatestCodeFor(phone);

        var (status, data, error) = await SendAsync(anon, HttpMethod.Post, "/api/v1/companies/onboarding",
            new
            {
                phoneNumber = phone,
                otpCode = code,
                fullName,
                companyName,
                companyType,
                city,
                address = (string?)null,
                bio = (string?)null
            });

        status.Should().Be(HttpStatusCode.OK, "onboarding should succeed ({0})", error?.ToString());
        var access = data!.Value.GetProperty("accessToken").GetString()!;
        var refresh = data.Value.GetProperty("refreshToken").GetString()!;
        return new TokenResult(access, refresh);
    }

    public static async Task<TokenResult> LoginAsync(TestAppFactory factory, string phone)
    {
        var anon = factory.CreateClient();
        await RequestOtpAsync(anon, phone, "Login");
        var code = factory.Sms.LatestCodeFor(phone);

        var (status, data, error) = await SendAsync(anon, HttpMethod.Post, "/api/v1/auth/otp/verify",
            new { phoneNumber = phone, code });

        status.Should().Be(HttpStatusCode.OK, "login should succeed ({0})", error?.ToString());
        return new TokenResult(
            data!.Value.GetProperty("accessToken").GetString()!,
            data.Value.GetProperty("refreshToken").GetString()!);
    }

    /// <summary>Seeds an admin user directly (bypasses onboarding) and returns an authenticated client.</summary>
    public static async Task<HttpClient> CreateAdminClientAsync(TestAppFactory factory, string phone = "+963900000099")
    {
        await factory.WithDbAsync(async db =>
        {
            if (!db.Users.Any(u => u.PhoneNumber == phone))
            {
                db.Users.Add(User.Create(phone, "Platform Admin", UserRole.Admin, null, factory.Clock.UtcNow));
                await db.SaveChangesAsync();
            }

            return true;
        });

        var tokens = await LoginAsync(factory, phone);
        return factory.WithToken(tokens.AccessToken);
    }

    public static async Task<Guid> FindCompanyIdByNameAsync(HttpClient adminClient, string companyName)
    {
        var (status, data, _) = await SendAsync(adminClient, HttpMethod.Get, "/api/v1/companies/pending?pageSize=50");
        status.Should().Be(HttpStatusCode.OK);

        foreach (var item in data!.Value.GetProperty("items").EnumerateArray())
        {
            if (item.GetProperty("name").GetString() == companyName)
            {
                return item.GetProperty("id").GetGuid();
            }
        }

        throw new InvalidOperationException($"Pending company '{companyName}' not found.");
    }

    public static async Task<Guid> CreateActiveProductAsync(
        TestAppFactory factory,
        HttpClient supplierClient,
        string title = "Active product")
    {
        var categoryId = (await SendAsync(supplierClient, HttpMethod.Get, "/api/v1/categories"))
            .Data!.Value[0].GetProperty("id").GetGuid();

        object Body(string status) => new
        {
            categoryId,
            title,
            description = (string?)null,
            moq = 10m,
            unitOfMeasure = "Kg",
            indicativePrice = (decimal?)null,
            currency = (string?)null,
            gsm = 160,
            gsmTolerancePct = 5,
            weaveStructure = "Plain",
            widthCm = 150,
            colorFamily = (string?)null,
            weightPerMeterG = (int?)null,
            careNotes = (string?)null,
            composition = new object[] { new { fiberType = "Cotton", percentage = 100m } },
            status
        };

        var (createStatus, createData, createError) = await SendAsync(
            supplierClient, HttpMethod.Post, "/api/v1/products", Body("Draft"));
        createStatus.Should().Be(HttpStatusCode.OK, "{0}", createError?.ToString());
        var productId = createData!.Value.GetProperty("id").GetGuid();

        var (updateStatus, _, updateError) = await SendAsync(
            supplierClient, HttpMethod.Put, $"/api/v1/products/{productId}", Body("Active"));
        updateStatus.Should().Be(HttpStatusCode.OK, "{0}", updateError?.ToString());

        return productId;
    }

    public static async Task VerifyCompanyAsync(HttpClient adminClient, Guid companyId)
    {
        var (status, _, error) = await SendAsync(adminClient, HttpMethod.Post,
            $"/api/v1/companies/{companyId}/verify", new { approved = true });
        status.Should().Be(HttpStatusCode.OK, "verification should succeed ({0})", error?.ToString());
    }

    public static async Task<HttpClient> VerifiedSupplierClientAsync(
        TestAppFactory factory,
        string phone,
        string companyName,
        string city)
    {
        var tokens = await OnboardAsync(factory, phone, "Supplier Owner", companyName, "Supplier", city);
        var client = factory.WithToken(tokens.AccessToken);
        var admin = await CreateAdminClientAsync(factory);
        var companyId = await FindCompanyIdByNameAsync(admin, companyName);
        await VerifyCompanyAsync(admin, companyId);
        return client;
    }

    public static async Task<(HttpClient Client, Guid CompanyId)> VerifiedBuyerAsync(
        TestAppFactory factory,
        string phone,
        string companyName,
        string city)
    {
        var tokens = await OnboardAsync(factory, phone, "Buyer Owner", companyName, "Buyer", city);
        var client = factory.WithToken(tokens.AccessToken);
        var admin = await CreateAdminClientAsync(factory);
        var companyId = await FindCompanyIdByNameAsync(admin, companyName);
        await VerifyCompanyAsync(admin, companyId);
        return (client, companyId);
    }
}

public sealed record TokenResult(string AccessToken, string RefreshToken);
