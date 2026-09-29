using System.Globalization;
using FluentValidation;
using Khyout.Application.Abstractions;
using Khyout.Application.Common;
using Khyout.Application.Common.Cqrs;
using Khyout.Domain.Common;
using Khyout.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Khyout.Application.Features.Catalog;

public sealed record SearchProductsQuery(
    Guid? CategoryId = null,
    int? GsmMin = null,
    int? GsmMax = null,
    string[]? Fibers = null,
    decimal? MoqMax = null,
    string? City = null,
    string Sort = "newest",
    int PageNumber = 1,
    int PageSize = 20) : IQuery<PagedResult<ProductSummaryDto>>;

public sealed class SearchProductsQueryValidator : AbstractValidator<SearchProductsQuery>
{
    private static readonly string[] AllowedSorts = { "newest", "moqAsc", "gsmAsc" };

    public SearchProductsQueryValidator()
    {
        RuleFor(q => q.Sort)
            .Must(s => AllowedSorts.Contains(s))
            .WithMessage("Sort must be one of: newest, moqAsc, gsmAsc.");
        RuleFor(q => q.GsmMin).GreaterThan(0).When(q => q.GsmMin.HasValue);
        RuleFor(q => q.GsmMax).GreaterThan(0).When(q => q.GsmMax.HasValue);
        RuleFor(q => q.MoqMax).GreaterThan(0).When(q => q.MoqMax.HasValue);
    }
}

public sealed class SearchProductsQueryHandler(IAppDbContext db)
    : IQueryHandler<SearchProductsQuery, PagedResult<ProductSummaryDto>>
{
    public async Task<PagedResult<ProductSummaryDto>> Handle(
        SearchProductsQuery query,
        CancellationToken cancellationToken)
    {
        var (page, size) = Pagination.Normalize(query.PageNumber, query.PageSize);

        var products = db.Products
            .AsNoTracking()
            .Where(p => p.Status == ProductStatus.Active)
            .Where(p => p.SupplierCompany.VerificationStatus == VerificationStatus.Verified);

        if (query.CategoryId is { } categoryId)
        {
            products = products.Where(p => p.CategoryId == categoryId);
        }

        if (query.GsmMin is { } gsmMin)
        {
            products = products.Where(p => p.Attributes != null && p.Attributes.Gsm >= gsmMin);
        }

        if (query.GsmMax is { } gsmMax)
        {
            products = products.Where(p => p.Attributes != null && p.Attributes.Gsm <= gsmMax);
        }

        if (query.MoqMax is { } moqMax)
        {
            products = products.Where(p => p.Moq <= moqMax);
        }

        if (!string.IsNullOrWhiteSpace(query.City))
        {
            var city = query.City.Trim();
            products = products.Where(p => p.SupplierCompany.City == city);
        }

        if (query.Fibers is { Length: > 0 })
        {
            foreach (var spec in query.Fibers)
            {
                var (fiber, minPercentage) = ParseFiberSpec(spec);
                products = products.Where(p =>
                    p.Composition.Any(c => c.FiberType == fiber && c.Percentage >= minPercentage));
            }
        }

        var total = await products.LongCountAsync(cancellationToken);

        products = query.Sort switch
        {
            "moqAsc" => products.OrderBy(p => p.Moq).ThenByDescending(p => p.CreatedAt),
            "gsmAsc" => products.OrderBy(p => p.Attributes!.Gsm).ThenByDescending(p => p.CreatedAt),
            _ => products.OrderByDescending(p => p.CreatedAt)
        };

        var items = await products
            .Skip((page - 1) * size)
            .Take(size)
            .Select(p => new ProductSummaryDto(
                p.Id,
                p.Title,
                p.CategoryId,
                p.Category.NameEn,
                p.Moq,
                p.UnitOfMeasure.ToString(),
                p.IndicativePrice,
                p.Currency,
                p.Attributes != null ? (int?)p.Attributes.Gsm : null,
                p.SupplierCompanyId,
                p.SupplierCompany.Name,
                p.SupplierCompany.City,
                p.CreatedAt))
            .ToListAsync(cancellationToken);

        return PagedResult<ProductSummaryDto>.Create(items, page, size, total);
    }

    private static (FiberType Fiber, decimal MinPercentage) ParseFiberSpec(string spec)
    {
        var parts = spec.Split(':', 2);
        if (parts.Length == 2 &&
            Enum.TryParse<FiberType>(parts[0].Trim(), ignoreCase: true, out var fiber) &&
            decimal.TryParse(parts[1].Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var percentage) &&
            percentage is > 0 and <= 100)
        {
            return (fiber, percentage);
        }

        throw new DomainRuleException(
            "catalog_filter_invalid",
            $"Invalid fiber filter '{spec}'. Expected format: 'Cotton:90' (fiber name and minimum percentage).");
    }
}
