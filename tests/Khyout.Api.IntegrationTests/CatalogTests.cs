using System.Net;
using FluentAssertions;
using Xunit;

namespace Khyout.Api.IntegrationTests;

public class CatalogTests : IClassFixture<TestAppFactory>
{
    private readonly TestAppFactory _factory;

    public CatalogTests(TestAppFactory factory)
    {
        _factory = factory;
        _factory.InitializeDatabase();
    }

    private static async Task<Guid> GetFirstCategoryIdAsync(HttpClient client)
    {
        var (status, data, _) = await TestApi.SendAsync(client, HttpMethod.Get, "/api/v1/categories");
        status.Should().Be(HttpStatusCode.OK);
        return data!.Value[0].GetProperty("id").GetGuid();
    }

    private async Task<HttpClient> CreateVerifiedSupplierWithProductAsync(
        string phone,
        string companyName,
        string city,
        string title,
        int gsm,
        decimal moq,
        bool publish)
    {
        var tokens = await TestApi.OnboardAsync(_factory, phone, "Supplier Owner", companyName, "Supplier", city);
        var client = _factory.WithToken(tokens.AccessToken);

        var admin = await TestApi.CreateAdminClientAsync(_factory);
        var companyId = await TestApi.FindCompanyIdByNameAsync(admin, companyName);
        await TestApi.VerifyCompanyAsync(admin, companyId);

        var categoryId = await GetFirstCategoryIdAsync(client);

        object Body(string status) => new
        {
            categoryId,
            title,
            description = (string?)null,
            moq,
            unitOfMeasure = "Kg",
            indicativePrice = (decimal?)null,
            currency = (string?)null,
            gsm,
            gsmTolerancePct = 5,
            weaveStructure = "Plain",
            widthCm = 150,
            colorFamily = (string?)null,
            weightPerMeterG = (int?)null,
            careNotes = (string?)null,
            composition = new object[]
            {
                new { fiberType = "Cotton", percentage = 95m },
                new { fiberType = "Lycra", percentage = 5m }
            },
            status
        };

        var (createStatus, createData, createError) = await TestApi.SendAsync(
            client, HttpMethod.Post, "/api/v1/products", Body("Draft"));
        createStatus.Should().Be(HttpStatusCode.OK, "product creation should succeed ({0})", createError?.ToString());
        var productId = createData!.Value.GetProperty("id").GetGuid();

        if (publish)
        {
            var (updateStatus, _, updateError) = await TestApi.SendAsync(
                client, HttpMethod.Put, $"/api/v1/products/{productId}", Body("Active"));
            updateStatus.Should().Be(HttpStatusCode.OK, "publishing should succeed ({0})", updateError?.ToString());
        }

        return client;
    }

    [Fact]
    public async Task Products_are_searchable_with_filters_caps_and_visibility()
    {
        await CreateVerifiedSupplierWithProductAsync("+963900300001", "Aleppo Cotton Co", "aleppo", "Cotton Jersey 180", 180, 25m, publish: true);
        await CreateVerifiedSupplierWithProductAsync("+963900300002", "Homs Denim Works", "homs", "Heavy Denim 400", 400, 60m, publish: true);
        await CreateVerifiedSupplierWithProductAsync("+963900300003", "Hidden Drafts", "homs", "Draft Wool 300", 300, 45m, publish: false);

        var buyer = await TestApi.OnboardAsync(_factory, "+963900300004", "Catalog Buyer", "Catalog Buyers", "Buyer", "damascus");
        var buyerClient = _factory.WithToken(buyer.AccessToken);

        var (allStatus, allData, _) = await TestApi.SendAsync(buyerClient, HttpMethod.Get, "/api/v1/products?pageSize=100");
        allStatus.Should().Be(HttpStatusCode.OK);
        allData!.Value.GetProperty("pageSize").GetInt32().Should().Be(50, "the page size cap must be enforced");
        allData.Value.GetProperty("items").GetArrayLength().Should().Be(2, "draft products must not appear in search");

        var (gsmStatus, gsmData, _) = await TestApi.SendAsync(buyerClient, HttpMethod.Get, "/api/v1/products?gsmMin=350&gsmMax=450");
        gsmStatus.Should().Be(HttpStatusCode.OK);
        gsmData!.Value.GetProperty("items").GetArrayLength().Should().Be(1);
        gsmData.Value.GetProperty("items")[0].GetProperty("title").GetString().Should().Be("Heavy Denim 400");

        var (cityStatus, cityData, _) = await TestApi.SendAsync(buyerClient, HttpMethod.Get, "/api/v1/products?city=aleppo");
        cityStatus.Should().Be(HttpStatusCode.OK);
        cityData!.Value.GetProperty("items").GetArrayLength().Should().Be(1);
        cityData.Value.GetProperty("items")[0].GetProperty("title").GetString().Should().Be("Cotton Jersey 180");

        var (fiberStrictStatus, fiberStrictData, _) = await TestApi.SendAsync(buyerClient, HttpMethod.Get, "/api/v1/products?fibers=Cotton:100");
        fiberStrictStatus.Should().Be(HttpStatusCode.OK);
        fiberStrictData!.Value.GetProperty("items").GetArrayLength().Should().Be(0, "products are 95% cotton");

        var (fiberOkStatus, fiberOkData, _) = await TestApi.SendAsync(buyerClient, HttpMethod.Get, "/api/v1/products?fibers=Cotton:90");
        fiberOkStatus.Should().Be(HttpStatusCode.OK);
        fiberOkData!.Value.GetProperty("items").GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task Invalid_composition_sum_is_rejected()
    {
        var tokens = await TestApi.OnboardAsync(_factory, "+963900300005", "Bad Sum", "Bad Sum Mills", "Supplier", "hama");
        var client = _factory.WithToken(tokens.AccessToken);

        var admin = await TestApi.CreateAdminClientAsync(_factory);
        var companyId = await TestApi.FindCompanyIdByNameAsync(admin, "Bad Sum Mills");
        await TestApi.VerifyCompanyAsync(admin, companyId);

        var categoryId = await GetFirstCategoryIdAsync(client);

        var (status, _, error) = await TestApi.SendAsync(client, HttpMethod.Post, "/api/v1/products",
            new
            {
                categoryId,
                title = "Bad",
                description = (string?)null,
                moq = 10m,
                unitOfMeasure = "Kg",
                indicativePrice = (decimal?)null,
                currency = (string?)null,
                gsm = 100,
                gsmTolerancePct = 5,
                weaveStructure = "Plain",
                widthCm = 100,
                colorFamily = (string?)null,
                weightPerMeterG = (int?)null,
                careNotes = (string?)null,
                composition = new object[] { new { fiberType = "Cotton", percentage = 50m } }
            });

        status.Should().Be(HttpStatusCode.Conflict);
        error!.Value.GetProperty("code").GetString().Should().Be("composition_sum_invalid");
    }
}
