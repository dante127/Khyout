using Khyout.Api.Contracts;
using Khyout.Application.Common.Cqrs;
using Khyout.Application.Features.Quotations;
using Khyout.Application.Features.Rfqs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Khyout.Api.Controllers;

[ApiController]
[Route("api/v1/quotations")]
[Authorize]
public sealed class QuotationsController(ISender sender) : ControllerBase
{
    [Authorize(Policy = "BuyerOnly")]
    [HttpPost("{id:guid}/accept")]
    public async Task<ActionResult<ApiResponse<RfqDetailDto>>> Accept(Guid id, CancellationToken cancellationToken)
        => Ok(ApiResponse.Ok(await sender.Send(new AcceptQuotationCommand(id), cancellationToken)));

    [Authorize(Policy = "BuyerOnly")]
    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<ApiResponse<object?>>> Reject(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new RejectQuotationCommand(id), cancellationToken);
        return Ok(ApiResponse.OkEmpty());
    }

    [Authorize(Policy = "SupplierOnly")]
    [HttpPost("{id:guid}/withdraw")]
    public async Task<ActionResult<ApiResponse<object?>>> Withdraw(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new WithdrawQuotationCommand(id), cancellationToken);
        return Ok(ApiResponse.OkEmpty());
    }
}
