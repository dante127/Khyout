using System.Net;
using FluentAssertions;
using Xunit;

namespace Khyout.Api.IntegrationTests;

public class SampleTests : IClassFixture<TestAppFactory>
{
    private readonly TestAppFactory _factory;

    public SampleTests(TestAppFactory factory)
    {
        _factory = factory;
        _factory.InitializeDatabase();
    }

    private static async Task<Guid> CreateActiveProductAsync(HttpClient supplierClient)
    {
        var categoryId = (await TestApi.SendAsync(supplierClient, HttpMethod.Get, "/api/v1/categories"))
            .Data!.Value[0].GetProperty("id").GetGuid();

        object Body(string status) => new
        {
            categoryId,
            title = "Sample product",
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

        var (createStatus, createData, createError) = await TestApi.SendAsync(
            supplierClient, HttpMethod.Post, "/api/v1/products", Body("Draft"));
        createStatus.Should().Be(HttpStatusCode.OK, "{0}", createError?.ToString());
        var productId = createData!.Value.GetProperty("id").GetGuid();

        var (updateStatus, _, updateError) = await TestApi.SendAsync(
            supplierClient, HttpMethod.Put, $"/api/v1/products/{productId}", Body("Active"));
        updateStatus.Should().Be(HttpStatusCode.OK, "{0}", updateError?.ToString());

        return productId;
    }

    [Fact]
    public async Task Sample_lifecycle_enforces_transitions_and_privacy()
    {
        var supplierClient = await TestApi.VerifiedSupplierClientAsync(_factory, "+963900600001", "Sample Supplier", "aleppo");
        var productId = await CreateActiveProductAsync(supplierClient);

        var buyer = await TestApi.OnboardAsync(_factory, "+963900600002", "Sample Buyer", "Sample Buyers", "Buyer", "damascus");
        var buyerClient = _factory.WithToken(buyer.AccessToken);

        var (createStatus, createData, createError) = await TestApi.SendAsync(buyerClient, HttpMethod.Post, "/api/v1/samples",
            new { productId, quantity = 2m, deliveryCity = "damascus", note = (string?)null, rfqQuotationId = (Guid?)null });
        createStatus.Should().Be(HttpStatusCode.OK, "{0}", createError?.ToString());
        var sampleId = createData!.Value.GetProperty("id").GetGuid();
        createData.Value.GetProperty("status").GetString().Should().Be("Requested");

        // Invalid transition: ship before approve.
        var (badStatus, _, badError) = await TestApi.SendAsync(
            supplierClient, HttpMethod.Patch, $"/api/v1/samples/{sampleId}/status", new { status = "Shipped" });
        badStatus.Should().Be(HttpStatusCode.Conflict);
        badError!.Value.GetProperty("code").GetString().Should().Be("sample_invalid_transition");

        // Happy path: approve → ship → receive.
        foreach (var target in new[] { "Approved", "Shipped", "Received" })
        {
            var (status, data, error) = await TestApi.SendAsync(
                supplierClient, HttpMethod.Patch, $"/api/v1/samples/{sampleId}/status", new { status = target });
            status.Should().Be(HttpStatusCode.OK, "transition to {0} failed ({1})", target, error?.ToString());
            data!.Value.GetProperty("status").GetString().Should().Be(target);
        }

        // Privacy: unrelated suppliers and buyers do not see this sample.
        var otherSupplier = await TestApi.VerifiedSupplierClientAsync(_factory, "+963900600003", "Other Supplier", "homs");
        var (otherSupplierStatus, otherSupplierData, _) = await TestApi.SendAsync(otherSupplier, HttpMethod.Get, "/api/v1/samples");
        otherSupplierStatus.Should().Be(HttpStatusCode.OK);
        otherSupplierData!.Value.GetProperty("items").GetArrayLength().Should().Be(0);

        var otherBuyer = await TestApi.OnboardAsync(_factory, "+963900600004", "Other Buyer", "Other Buyers", "Buyer", "homs");
        var otherBuyerClient = _factory.WithToken(otherBuyer.AccessToken);
        var (otherBuyerStatus, otherBuyerData, _) = await TestApi.SendAsync(otherBuyerClient, HttpMethod.Get, "/api/v1/samples");
        otherBuyerStatus.Should().Be(HttpStatusCode.OK);
        otherBuyerData!.Value.GetProperty("items").GetArrayLength().Should().Be(0);

        // The requesting buyer sees their sample.
        var (mineStatus, mineData, _) = await TestApi.SendAsync(buyerClient, HttpMethod.Get, "/api/v1/samples");
        mineStatus.Should().Be(HttpStatusCode.OK);
        mineData!.Value.GetProperty("items").GetArrayLength().Should().BeGreaterThanOrEqualTo(1);
    }
}
