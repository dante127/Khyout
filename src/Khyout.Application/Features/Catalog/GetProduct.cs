using Khyout.Application.Abstractions;
using Khyout.Application.Common;
using Khyout.Application.Common.Cqrs;
using Khyout.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Khyout.Application.Features.Catalog;

public sealed record GetProductQuery(Guid ProductId) : IQuery<ProductDetailDto>;

public sealed class GetProductQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    : IQueryHandler<GetProductQuery, ProductDetailDto>
{
    public async Task<ProductDetailDto> Handle(GetProductQuery query, CancellationToken cancellationToken)
    {
        var product = await db.Products
            .AsNoTracking()
            .Include(p => p.Attributes)
            .Include(p => p.Composition)
            .Include(p => p.Images)
            .Include(p => p.SupplierCompany)
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == query.ProductId, cancellationToken)
            ?? throw new NotFoundException("Product not found.");

        var isOwner = currentUser.CompanyId is { } companyId && companyId == product.SupplierCompanyId;
        var isAdmin = currentUser.Role == UserRole.Admin;

        // Non-active products are visible only to their owner and admins.
        if (product.Status != ProductStatus.Active && !isOwner && !isAdmin)
        {
            throw new NotFoundException("Product not found.");
        }

        return ProductDetailDto.From(product);
    }
}
