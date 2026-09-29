using Khyout.Api.Contracts;
using Khyout.Application.Common;
using Khyout.Application.Common.Cqrs;
using Khyout.Application.Features.Samples;
using Khyout.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Khyout.Api.Controllers;

[ApiController]
[Route("api/v1/samples")]
[Authorize]
public sealed class SamplesController(ISender sender) : ControllerBase
{
    [Authorize(Policy = "BuyerOnly")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<SampleDto>>> Create(
        CreateSampleRequestCommand command,
        CancellationToken cancellationToken)
        => Ok(ApiResponse.Ok(await sender.Send(command, cancellationToken)));

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<SampleDto>>>> List(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
        => Ok(ApiResponse.Ok(await sender.Send(new ListSampleRequestsQuery(pageNumber, pageSize, status), cancellationToken)));

    [Authorize(Policy = "SupplierOrAdmin")]
    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<SampleDto>>> UpdateStatus(
        Guid id,
        UpdateSampleStatusBody body,
        CancellationToken cancellationToken)
        => Ok(ApiResponse.Ok(await sender.Send(new UpdateSampleStatusCommand(id, body.Status), cancellationToken)));
}

public sealed record UpdateSampleStatusBody(SampleStatus Status);
