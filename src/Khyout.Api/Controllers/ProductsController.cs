using Khyout.Api.Contracts;
using Khyout.Application.Common;
using Khyout.Application.Common.Cqrs;
using Khyout.Application.Features.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Khyout.Api.Controllers;

[ApiController]
[Route("api/v1/products")]
[Authorize]
public sealed class ProductsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ProductSummaryDto>>>> Search(
        [FromQuery] SearchProductsQuery query,
        CancellationToken cancellationToken)
        => Ok(ApiResponse.Ok(await sender.Send(query, cancellationToken)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProductDetailDto>>> Get(Guid id, CancellationToken cancellationToken)
        => Ok(ApiResponse.Ok(await sender.Send(new GetProductQuery(id), cancellationToken)));

    [Authorize(Policy = "VerifiedSupplier")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProductDetailDto>>> Create(
        CreateProductCommand command,
        CancellationToken cancellationToken)
        => Ok(ApiResponse.Ok(await sender.Send(command, cancellationToken)));

    [Authorize(Policy = "VerifiedSupplier")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProductDetailDto>>> Update(
        Guid id,
        UpdateProductCommand command,
        CancellationToken cancellationToken)
        => Ok(ApiResponse.Ok(await sender.Send(command with { ProductId = id }, cancellationToken)));
}
