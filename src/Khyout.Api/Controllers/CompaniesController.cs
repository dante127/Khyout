using Khyout.Api.Contracts;
using Khyout.Application.Common;
using Khyout.Application.Common.Cqrs;
using Khyout.Application.Features.Auth;
using Khyout.Application.Features.Companies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Khyout.Api.Controllers;

[ApiController]
[Route("api/v1/companies")]
[Authorize]
public sealed class CompaniesController(ISender sender) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<CompanyDto>>> Me(CancellationToken cancellationToken)
        => Ok(ApiResponse.Ok(await sender.Send(new GetMyCompanyQuery(), cancellationToken)));

    [HttpPut("me")]
    public async Task<ActionResult<ApiResponse<CompanyDto>>> UpdateMe(UpdateMyCompanyCommand command, CancellationToken cancellationToken)
        => Ok(ApiResponse.Ok(await sender.Send(command, cancellationToken)));

    [AllowAnonymous]
    [EnableRateLimiting("otp")]
    [HttpPost("onboarding")]
    public async Task<ActionResult<ApiResponse<TokenPairDto>>> Onboard(OnboardCompanyCommand command, CancellationToken cancellationToken)
        => Ok(ApiResponse.Ok(await sender.Send(command, cancellationToken)));

    [Authorize(Policy = "AdminOnly")]
    [HttpGet("pending")]
    public async Task<ActionResult<ApiResponse<PagedResult<CompanyDto>>>> Pending(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
        => Ok(ApiResponse.Ok(await sender.Send(new ListPendingCompaniesQuery(pageNumber, pageSize), cancellationToken)));

    [Authorize(Policy = "AdminOnly")]
    [HttpPost("{id:guid}/verify")]
    public async Task<ActionResult<ApiResponse<CompanyDto>>> Verify(
        Guid id,
        VerifyCompanyRequest request,
        CancellationToken cancellationToken)
        => Ok(ApiResponse.Ok(await sender.Send(new VerifyCompanyCommand(id, request.Approved), cancellationToken)));
}

public sealed record VerifyCompanyRequest(bool Approved);
