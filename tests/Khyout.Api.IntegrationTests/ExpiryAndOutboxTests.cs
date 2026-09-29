using System.Net;
using FluentAssertions;
using Khyout.Domain.Enums;
using Khyout.Infrastructure.BackgroundJobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Khyout.Api.IntegrationTests;

public class ExpiryTests : IClassFixture<TestAppFactory>
{
    private readonly TestAppFactory _factory;

    public ExpiryTests(TestAppFactory factory)
    {
        _factory = factory;
        _factory.InitializeDatabase();
    }

    [Fact]
    public async Task Expiration_processor_expires_overdue_bids_and_rfqs()
    {
        var (buyerClient, _) = await TestApi.VerifiedBuyerAsync(_factory, "+963900500001", "Expiry Buyers", "damascus");
        var supplier = await TestApi.VerifiedSupplierClientAsync(_factory, "+963900500002", "Expiry Mills", "homs");

        var categoryId = (await TestApi.SendAsync(buyerClient, HttpMethod.Get, "/api/v1/categories"))
            .Data!.Value[0].GetProperty("id").GetGuid();

        var (_, createData, _) = await TestApi.SendAsync(buyerClient, HttpMethod.Post, "/api/v1/rfqs",
            new
            {
                categoryId,
                title = "Expiry probe RFQ",
                description = (string?)null,
                quantityNeeded = 10m,
                unitOfMeasure = "Kg",
                targetDeliveryDate = "2026-12-01",
                closingDate = "2026-12-15T00:00:00+00:00"
            });
        var rfqId = createData!.Value.GetProperty("id").GetGuid();

        // Bid valid for 1 minute (TestClock starts at 12:00Z).
        var (bidStatus, bidData, bidError) = await TestApi.SendAsync(supplier, HttpMethod.Post, $"/api/v1/rfqs/{rfqId}/quotations",
            new { unitPrice = 2m, currency = "USD", validUntil = "2026-09-29T12:01:00+00:00", leadTimeDays = 5, note = (string?)null });
        bidStatus.Should().Be(HttpStatusCode.OK, "bid should be accepted ({0})", bidError?.ToString());
        var quoteId = bidData!.Value.GetProperty("quotationId").GetGuid();

        // Advance past validity: before the worker runs, accepting must already fail with
        // quotation_expired (accept-time re-validation).
        _factory.Clock.Advance(TimeSpan.FromMinutes(2));
        var (acceptStatus, _, acceptError) = await TestApi.SendAsync(buyerClient, HttpMethod.Post, $"/api/v1/quotations/{quoteId}/accept");
        acceptStatus.Should().Be(HttpStatusCode.Conflict);
        acceptError!.Value.GetProperty("code").GetString().Should().Be("quotation_expired");

        // Run the processor: the bid is marked Expired and the supplier can see it.
        using (var scope = _factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<ExpirationProcessor>().ProcessAsync();
        }

        var (listStatus, listData, _) = await TestApi.SendAsync(supplier, HttpMethod.Get, $"/api/v1/rfqs/{rfqId}/quotations");
        listStatus.Should().Be(HttpStatusCode.OK);
        listData!.Value.GetProperty("items")[0].GetProperty("status").GetString().Should().Be("Expired");

        // Advance past RFQ closing and run again: the RFQ expires.
        _factory.Clock.Advance(TimeSpan.FromDays(120));
        using (var scope = _factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<ExpirationProcessor>().ProcessAsync();
        }

        var (rfqStatus, rfqData, _) = await TestApi.SendAsync(buyerClient, HttpMethod.Get, $"/api/v1/rfqs/{rfqId}");
        rfqStatus.Should().Be(HttpStatusCode.OK);
        rfqData!.Value.GetProperty("rfq").GetProperty("status").GetString().Should().Be("Expired");
    }
}

public class OutboxTests : IClassFixture<TestAppFactory>
{
    private readonly TestAppFactory _factory;

    public OutboxTests(TestAppFactory factory)
    {
        _factory = factory;
        _factory.InitializeDatabase();
    }

    [Fact]
    public async Task Outbox_dispatcher_marks_sent_and_retries_on_failure()
    {
        var (buyerClient, _) = await TestApi.VerifiedBuyerAsync(_factory, "+963900500003", "Outbox Buyers", "damascus");

        var categoryId = (await TestApi.SendAsync(buyerClient, HttpMethod.Get, "/api/v1/categories"))
            .Data!.Value[0].GetProperty("id").GetGuid();

        await TestApi.SendAsync(buyerClient, HttpMethod.Post, "/api/v1/rfqs",
            new
            {
                categoryId,
                title = "Outbox RFQ",
                description = (string?)null,
                quantityNeeded = 5m,
                unitOfMeasure = "Kg",
                targetDeliveryDate = "2026-12-01",
                closingDate = "2026-12-15T00:00:00+00:00"
            });

        // Failure pass: attempts increment, message stays Pending.
        _factory.Telegram.FailAll = true;
        using (var scope = _factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<OutboxDispatcher>().ProcessAsync();
        }

        _factory.Telegram.FailAll = false;

        var (attempts, stillPending) = await _factory.WithDbAsync(async db =>
        {
            var message = await db.OutboxMessages
                .Where(m => m.Type == OutboxMessageType.RfqCreated)
                .OrderByDescending(m => m.CreatedAt)
                .FirstAsync();
            return (message.Attempts, message.Status == OutboxMessageStatus.Pending);
        });

        attempts.Should().BeGreaterThanOrEqualTo((short)1);
        stillPending.Should().BeTrue("the message must not be dropped on a failed delivery");

        // Move past the retry backoff and deliver.
        _factory.Clock.Advance(TimeSpan.FromMinutes(1));
        using (var scope = _factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<OutboxDispatcher>().ProcessAsync();
        }

        var sent = await _factory.WithDbAsync(async db =>
            await db.OutboxMessages.AnyAsync(m =>
                m.Type == OutboxMessageType.RfqCreated && m.Status == OutboxMessageStatus.Sent));

        sent.Should().BeTrue();
        _factory.Telegram.Sent.Should().NotBeEmpty();
    }
}
