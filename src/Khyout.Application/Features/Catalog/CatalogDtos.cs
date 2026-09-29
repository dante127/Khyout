using Khyout.Application.Abstractions;
using Khyout.Application.Common;
using Khyout.Application.Common.Cqrs;
using Khyout.Domain.Entities;
using Khyout.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Khyout.Application.Features.Catalog;

public sealed record CategoryNodeDto(
    Guid Id,
    string Slug,
    string NameAr,
    string NameEn,
    int SortOrder,
    IReadOnlyList<CategoryNodeDto> Children);

public sealed record ProductSummaryDto(
    Guid Id,
    string Title,
    Guid CategoryId,
    string CategoryName,
    decimal Moq,
    string UnitOfMeasure,
    decimal? IndicativePrice,
    string? Currency,
    int? Gsm,
    Guid SupplierCompanyId,
    string SupplierName,
    string SupplierCity,
    DateTimeOffset CreatedAt);

public sealed record FiberDto(string FiberType, decimal Percentage);

public sealed record ProductImageDto(Guid Id, string StoragePath, int WidthPx, int HeightPx, short SortOrder);

public sealed record ProductDetailDto(
    Guid Id,
    string Title,
    string? Description,
    Guid CategoryId,
    string CategoryName,
    decimal Moq,
    string UnitOfMeasure,
    decimal? IndicativePrice,
    string? Currency,
    string Status,
    int? Gsm,
    int? GsmTolerancePct,
    string? WeaveStructure,
    int? WidthCm,
    string? ColorFamily,
    int? WeightPerMeterG,
    string? CareNotes,
    IReadOnlyList<FiberDto> Composition,
    IReadOnlyList<ProductImageDto> Images,
    Guid SupplierCompanyId,
    string SupplierName,
    string SupplierCity,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static ProductDetailDto From(Product product) =>
        new(
            product.Id,
            product.Title,
            product.Description,
            product.CategoryId,
            product.Category?.NameEn ?? string.Empty,
            product.Moq,
            product.UnitOfMeasure.ToString(),
            product.IndicativePrice,
            product.Currency,
            product.Status.ToString(),
            product.Attributes?.Gsm,
            product.Attributes?.GsmTolerancePct,
            product.Attributes?.WeaveStructure.ToString(),
            product.Attributes?.WidthCm,
            product.Attributes?.ColorFamily,
            product.Attributes?.WeightPerMeterG,
            product.Attributes?.CareNotes,
            product.Composition.Select(c => new FiberDto(c.FiberType.ToString(), c.Percentage)).ToList(),
            product.Images.OrderBy(i => i.SortOrder).Select(i => new ProductImageDto(i.Id, i.StoragePath, i.WidthPx, i.HeightPx, i.SortOrder)).ToList(),
            product.SupplierCompanyId,
            product.SupplierCompany?.Name ?? string.Empty,
            product.SupplierCompany?.City ?? string.Empty,
            product.CreatedAt,
            product.UpdatedAt);
}

public sealed record GetCategoryTreeQuery : IQuery<IReadOnlyList<CategoryNodeDto>>;

public sealed class GetCategoryTreeQueryHandler(IAppDbContext db)
    : IQueryHandler<GetCategoryTreeQuery, IReadOnlyList<CategoryNodeDto>>
{
    public async Task<IReadOnlyList<CategoryNodeDto>> Handle(GetCategoryTreeQuery query, CancellationToken cancellationToken)
    {
        var categories = await db.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ToListAsync(cancellationToken);

        var byParent = categories
            .GroupBy(c => c.ParentId ?? Guid.Empty)
            .ToDictionary(g => g.Key, g => g.OrderBy(c => c.SortOrder).ToList());

        IReadOnlyList<CategoryNodeDto> Build(Guid parentId) =>
            byParent.TryGetValue(parentId, out var children)
                ? children
                    .Select(c => new CategoryNodeDto(c.Id, c.Slug, c.NameAr, c.NameEn, c.SortOrder, Build(c.Id)))
                    .ToList()
                : Array.Empty<CategoryNodeDto>();

        return Build(Guid.Empty);
    }
}
