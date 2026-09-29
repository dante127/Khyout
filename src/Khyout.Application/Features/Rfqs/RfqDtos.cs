using Khyout.Domain.Entities;

namespace Khyout.Application.Features.Rfqs;

public sealed record RfqSummaryDto(
    Guid Id,
    string Title,
    Guid CategoryId,
    string CategoryName,
    decimal QuantityNeeded,
    string UnitOfMeasure,
    DateTimeOffset ClosingDate,
    string Status,
    int BidCount,
    DateTimeOffset CreatedAt);

public sealed record RfqDetailDto(
    Guid Id,
    Guid BuyerCompanyId,
    string BuyerCompanyName,
    Guid CategoryId,
    string CategoryName,
    string Title,
    string? Description,
    decimal QuantityNeeded,
    string UnitOfMeasure,
    DateOnly TargetDeliveryDate,
    DateTimeOffset ClosingDate,
    string Status,
    Guid? AcceptedQuotationId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record RfqBidSummaryDto(
    Guid QuotationId,
    Guid SupplierCompanyId,
    string SupplierName,
    decimal UnitPrice,
    string Currency,
    DateTimeOffset ValidUntil,
    int LeadTimeDays,
    string? Note,
    string Status,
    DateTimeOffset CreatedAt);

public sealed record MyQuotationDto(
    Guid QuotationId,
    decimal UnitPrice,
    string Currency,
    DateTimeOffset ValidUntil,
    int LeadTimeDays,
    string? Note,
    string Status);

public static class RfqMapping
{
    public static RfqDetailDto ToDetail(RfqRequest rfq) => new(
        rfq.Id,
        rfq.BuyerCompanyId,
        rfq.BuyerCompany?.Name ?? string.Empty,
        rfq.CategoryId,
        rfq.Category?.NameEn ?? string.Empty,
        rfq.Title,
        rfq.Description,
        rfq.QuantityNeeded,
        rfq.UnitOfMeasure.ToString(),
        rfq.TargetDeliveryDate,
        rfq.ClosingDate,
        rfq.Status.ToString(),
        rfq.AcceptedQuotationId,
        rfq.CreatedAt,
        rfq.UpdatedAt);
}

public static class QuotationMapping
{
    public static RfqBidSummaryDto ToBidSummary(RfqQuotation quotation) => new(
        quotation.Id,
        quotation.SupplierCompanyId,
        quotation.SupplierCompany?.Name ?? string.Empty,
        quotation.UnitPrice,
        quotation.Currency,
        quotation.ValidUntil,
        quotation.LeadTimeDays,
        quotation.Note,
        quotation.Status.ToString(),
        quotation.CreatedAt);

    public static MyQuotationDto ToMine(RfqQuotation quotation) => new(
        quotation.Id,
        quotation.UnitPrice,
        quotation.Currency,
        quotation.ValidUntil,
        quotation.LeadTimeDays,
        quotation.Note,
        quotation.Status.ToString());
}
