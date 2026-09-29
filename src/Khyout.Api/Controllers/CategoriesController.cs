using Khyout.Api.Contracts;
using Khyout.Application.Common.Cqrs;
using Khyout.Application.Features.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Khyout.Api.Controllers;

[ApiController]
[Route("api/v1/categories")]
[Authorize]
public sealed class CategoriesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CategoryNodeDto>>>> Tree(CancellationToken cancellationToken)
        => Ok(ApiResponse.Ok(await sender.Send(new GetCategoryTreeQuery(), cancellationToken)));
}
