using System.Net;
using FluentAssertions;
using Xunit;

namespace Khyout.Api.IntegrationTests;

public class RfqLifecycleTests : IClassFixture<TestAppFactory>
{
    private readonly TestAppFactory _factory;

    public RfqLifecycleTests(TestAppFactory factory)
    {
        _factory = factory;
        _factory.InitializeDatabase();
    }

    [Fact]
    public async Task Full_rfq_lifecycle_enforces_blind_bidding_privacy()
    {
        // Verified buyer creates an RFQ; two verified suppliers bid; only the buyer sees all bids.
        var (buyerClient, _) = await TestApi.VerifiedBuyerAsync(
            _factory, "+963900400001", "Lifecycle Buyers", "damascus");

        var categoryId = (await TestApi.SendAsync(buyerClient, HttpMethod.Get, "/api/v1/categories"))
            .Data!.Value[0].GetProperty("id").GetGuid();

        var (createStatus, createData, createError) = await TestApi.SendAsync(buyerClient, HttpMethod.Post, "/api/v1/rfqs",
            new
            {
                categoryId,
                title = "Jersey 180gsm 500kg",
                description = (string?)null,
                quantityNeeded = 500m,
                unitOfMeasure = "Kg",
                targetDeliveryDate = "2026-12-01",
                closingDate = "2026-12-15T00:00:00+00:00"
            });

        createStatus.Should().Be(HttpStatusCode.OK, "RFQ creation should succeed ({0})", createError?.ToString());
        var rfqId = createData!.Value.GetProperty("id").GetGuid();

        var supplierA = await TestApi.VerifiedSupplierClientAsync(_factory, "+963900400002", "Supplier Alpha", "homs");
        var supplierB = await TestApi.VerifiedSupplierClientAsync(_factory, "+963900400003", "Supplier Beta", "homs");

        var (bidAStatus, _, _) = await TestApi.SendAsync(supplierA, HttpMethod.Post, $"/api/v1/rfqs/{rfqId}/quotations",
            new { unitPrice = 4.2m, currency = "USD", validUntil = "2026-12-10T00:00:00+00:00", leadTimeDays = 14, note = (string?)null });
        bidAStatus.Should().Be(HttpStatusCode.OK);

        var (bidBStatus, _, _) = await TestApi.SendAsync(supplierB, HttpMethod.Post, $"/api/v1/rfqs/{rfqId}/quotations",
            new { unitPrice = 3.9m, currency = "USD", validUntil = "2026-12-09T00:00:00+00:00", leadTimeDays = 10, note = (string?)null });
        bidBStatus.Should().Be(HttpStatusCode.OK);

        // Supplier A may only ever see their own bid.
        var (aListStatus, aData, _) = await TestApi.SendAsync(supplierA, HttpMethod.Get, $"/api/v1/rfqs/{rfqId}/quotations");
        aListStatus.Should().Be(HttpStatusCode.OK);
        aData!.Value.GetProperty("items").GetArrayLength().Should().Be(1);
        aData.Value.GetProperty("items")[0].GetProperty("unitPrice").GetDecimal().Should().Be(4.2m);

        // The buyer sees both bids.
        var (buyerListStatus, buyerData, _) = await TestApi.SendAsync(buyerClient, HttpMethod.Get, $"/api/v1/rfqs/{rfqId}/quotations");
        buyerListStatus.Should().Be(HttpStatusCode.OK);
        buyerData!.Value.GetProperty("items").GetArrayLength().Should().Be(2);

        // Anonymous callers cannot list bids at all.
        var (anonStatus, _, _) = await TestApi.SendAsync(_factory.CreateClient(), HttpMethod.Get, $"/api/v1/rfqs/{rfqId}/quotations");
        anonStatus.Should().Be(HttpStatusCode.Unauthorized);

        // Accept supplier A's bid.
        var aBidId = aData.Value.GetProperty("items")[0].GetProperty("quotationId").GetGuid();
        var (acceptStatus, acceptData, _) = await TestApi.SendAsync(buyerClient, HttpMethod.Post, $"/api/v1/quotations/{aBidId}/accept");
        acceptStatus.Should().Be(HttpStatusCode.OK);
        acceptData!.Value.GetProperty("status").GetString().Should().Be("Awarded");

        // Duplicate accept is rejected.
        var (againStatus, _, againError) = await TestApi.SendAsync(buyerClient, HttpMethod.Post, $"/api/v1/quotations/{aBidId}/accept");
        againStatus.Should().Be(HttpStatusCode.Conflict);
        againError!.Value.GetProperty("code").GetString().Should().Be("quotation_not_submitted");

        // RFQ detail shows Awarded; supplier A sees own (accepted) bid.
        var (detailStatus, detailData, _) = await TestApi.SendAsync(buyerClient, HttpMethod.Get, $"/api/v1/rfqs/{rfqId}");
        detailStatus.Should().Be(HttpStatusCode.OK);
        detailData!.Value.GetProperty("rfq").GetProperty("status").GetString().Should().Be("Awarded");

        var (aMineStatus, aMineData, _) = await TestApi.SendAsync(supplierA, HttpMethod.Get, $"/api/v1/rfqs/{rfqId}");
        aMineData!.Value.GetProperty("myQuotation").GetProperty("status").GetString().Should().Be("Accepted");
        aMineData.Value.GetProperty("bidCount").GetInt32().Should().Be(1);
    }
}
