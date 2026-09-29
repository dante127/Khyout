using Khyout.Api.Contracts;
using Khyout.Application.Common;
using Khyout.Application.Common.Cqrs;
using Khyout.Application.Features.Quotations;
using Khyout.Application.Features.Rfqs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Khyout.Api.Controllers;

[ApiController]
[Route("api/v1/rfqs")]
[Authorize]
public sealed class RfqsController(ISender sender) : ControllerBase
{
    [Authorize(Policy = "VerifiedBuyer")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<RfqDetailDto>>> Create(CreateRfqCommand command, CancellationToken cancellationToken)
        => Ok(ApiResponse.Ok(await sender.Send(command, cancellationToken)));

    [Authorize(Policy = "BuyerOnly")]
    [HttpGet("mine")]
    public async Task<ActionResult<ApiResponse<PagedResult<RfqSummaryDto>>>> Mine(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
        => Ok(ApiResponse.Ok(await sender.Send(new GetMyRfqsQuery(pageNumber, pageSize, status), cancellationToken)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<RfqDetailResult>>> Detail(Guid id, CancellationToken cancellationToken)
        => Ok(ApiResponse.Ok(await sender.Send(new GetRfqDetailQuery(id), cancellationToken)));

    [Authorize(Policy = "BuyerOnly")]
    [HttpPost("{id:guid}/extend")]
    public async Task<ActionResult<ApiResponse<object?>>> Extend(Guid id, ExtendRfqRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new ExtendClosingCommand(id, request.NewClosingDate), cancellationToken);
        return Ok(ApiResponse.OkEmpty());
    }

    [Authorize(Policy = "BuyerOnly")]
    [HttpPost("{id:guid}/close")]
    public async Task<ActionResult<ApiResponse<object?>>> Close(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new CloseRfqCommand(id), cancellationToken);
        return Ok(ApiResponse.OkEmpty());
    }

    [Authorize(Policy = "VerifiedSupplier")]
    [HttpPost("{id:guid}/quotations")]
    public async Task<ActionResult<ApiResponse<MyQuotationDto>>> Submit(
        Guid id,
        SubmitQuotationBody body,
        CancellationToken cancellationToken)
        => Ok(ApiResponse.Ok(await sender.Send(
            new SubmitQuotationCommand(id, body.UnitPrice, body.Currency, body.ValidUntil, body.LeadTimeDays, body.Note),
            cancellationToken)));

    [HttpGet("{id:guid}/quotations")]
    public async Task<ActionResult<ApiResponse<PagedResult<RfqBidSummaryDto>>>> Bids(
        Guid id,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
        => Ok(ApiResponse.Ok(await sender.Send(new GetQuotationsForRfqQuery(id, pageNumber, pageSize), cancellationToken)));
}

public sealed record ExtendRfqRequest(DateTimeOffset NewClosingDate);

public sealed record SubmitQuotationBody(
    decimal UnitPrice,
    string Currency,
    DateTimeOffset ValidUntil,
    int LeadTimeDays,
    string? Note);
