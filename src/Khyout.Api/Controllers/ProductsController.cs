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

    [Authorize(Policy = "VerifiedSupplier")]
    [HttpPost("{id:guid}/images")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<ProductImageDto>>> UploadImage(
        Guid id,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new ApiResponse<object?>(false, null, new ApiError("upload_empty", "No file was uploaded.")));
        }

        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, cancellationToken);

        var result = await sender.Send(
            new UploadProductImageCommand(id, buffer.ToArray(), file.ContentType, file.FileName),
            cancellationToken);

        return Ok(ApiResponse.Ok(result));
    }
}
