using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using Khyout.Infrastructure.Persistence;
using SkiaSharp;
using Xunit;

namespace Khyout.Api.IntegrationTests;

public class ImageUploadTests : IClassFixture<TestAppFactory>
{
    private readonly TestAppFactory _factory;

    public ImageUploadTests(TestAppFactory factory)
    {
        _factory = factory;
        _factory.InitializeDatabase();
    }

    private static byte[] CreatePng(int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(new SKColor(180, 40, 60));
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private async Task<(HttpStatusCode Status, JsonElement? Data, JsonElement? Error)> UploadAsync(
        HttpClient client,
        Guid productId,
        byte[] bytes,
        string contentType)
    {
        var content = new MultipartFormDataContent();
        var part = new ByteArrayContent(bytes);
        part.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(part, "file", "upload.png");

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/products/{productId}/images")
        {
            Content = content
        };

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

    private async Task<(HttpClient Client, Guid ProductId)> SupplierWithActiveProductAsync(
        string phone,
        string company,
        string city)
    {
        var client = await TestApi.VerifiedSupplierClientAsync(_factory, phone, company, city);
        var productId = await TestApi.CreateActiveProductAsync(_factory, client, "Product " + company);
        return (client, productId);
    }

    [Fact]
    public async Task Upload_transcodes_to_webp_within_budget_and_dedupes_by_hash()
    {
        var (client, productId) = await SupplierWithActiveProductAsync(
            "+963900700101", "Image Works", "aleppo");
        var png = CreatePng(2400, 1800);

        var (status, data, error) = await UploadAsync(client, productId, png, "image/png");
        status.Should().Be(HttpStatusCode.OK, "{0}", error?.ToString());
        var storagePath = data!.Value.GetProperty("storagePath").GetString()!;
        storagePath.Should().EndWith(".webp");
        var firstImageId = data.Value.GetProperty("id").GetGuid();

        var storedPath = Path.Combine(_factory.MediaRoot, storagePath);
        File.Exists(storedPath).Should().BeTrue();
        new FileInfo(storedPath).Length.Should().BeLessThanOrEqualTo(150 * 1024);

        using var decoded = SKBitmap.Decode(File.ReadAllBytes(storedPath));
        decoded.Should().NotBeNull();
        decoded.Width.Should().BeLessThanOrEqualTo(1600);
        decoded.Height.Should().BeLessThanOrEqualTo(1600);

        // Same content → dedupe by hash.
        var (dupStatus, dupData, _) = await UploadAsync(client, productId, png, "image/png");
        dupStatus.Should().Be(HttpStatusCode.OK);
        dupData!.Value.GetProperty("id").GetGuid().Should().Be(firstImageId);
    }

    [Fact]
    public async Task Corrupt_image_content_is_rejected()
    {
        var (client, productId) = await SupplierWithActiveProductAsync(
            "+963900700102", "Reject Works", "homs");
        var fake = CreatePng(10, 10).AsSpan(0, 40).ToArray(); // valid header, broken body

        var (status, _, error) = await UploadAsync(client, productId, fake, "image/png");

        status.Should().Be(HttpStatusCode.Conflict);
        error!.Value.GetProperty("code").GetString().Should().Be("image_decode_failed");
    }

    [Fact]
    public async Task Disallowed_content_type_is_rejected()
    {
        var (client, productId) = await SupplierWithActiveProductAsync(
            "+963900700103", "Type Works", "homs");
        var bytes = CreatePng(10, 10);

        var (status, _, error) = await UploadAsync(client, productId, bytes, "text/plain");

        status.Should().Be(HttpStatusCode.BadRequest);
        error!.Value.GetProperty("code").GetString().Should().Be("validation_failed");
    }

    [Fact]
    public async Task Non_owner_cannot_upload()
    {
        var (ownerClient, productId) = await SupplierWithActiveProductAsync(
            "+963900700104", "Owner Works", "homs");

        var outsiderClient = await TestApi.VerifiedSupplierClientAsync(_factory, "+963900700105", "Other Mills", "homs");

        var (status, _, error) = await UploadAsync(outsiderClient, productId, CreatePng(50, 50), "image/png");

        status.Should().Be(HttpStatusCode.Forbidden);
        error!.Value.GetProperty("message").GetString().Should().Contain("product owner");
    }
}
