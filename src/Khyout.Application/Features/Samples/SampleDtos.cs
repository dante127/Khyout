using Khyout.Domain.Entities;

namespace Khyout.Application.Features.Samples;

public sealed record SampleDto(
    Guid Id,
    Guid BuyerCompanyId,
    string BuyerCompanyName,
    Guid SupplierCompanyId,
    string SupplierCompanyName,
    Guid ProductId,
    string ProductTitle,
    Guid? RfqQuotationId,
    string Status,
    decimal Quantity,
    string? DeliveryCity,
    string? Note,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static SampleDto From(SampleRequest sample) => new(
        sample.Id,
        sample.BuyerCompanyId,
        sample.BuyerCompany?.Name ?? string.Empty,
        sample.SupplierCompanyId,
        sample.SupplierCompany?.Name ?? string.Empty,
        sample.ProductId,
        sample.Product?.Title ?? string.Empty,
        sample.RfqQuotationId,
        sample.Status.ToString(),
        sample.Quantity,
        sample.DeliveryCity,
        sample.Note,
        sample.CreatedAt,
        sample.UpdatedAt);
}
