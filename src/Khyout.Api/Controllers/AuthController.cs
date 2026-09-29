using Khyout.Api.Contracts;
using Khyout.Application.Common.Cqrs;
using Khyout.Application.Features.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Khyout.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    [AllowAnonymous]
    [EnableRateLimiting("otp")]
    [HttpPost("otp/request")]
    public async Task<ActionResult<ApiResponse<object?>>> RequestOtp(RequestOtpCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);
        return Ok(ApiResponse.OkEmpty());
    }

    [AllowAnonymous]
    [EnableRateLimiting("otp")]
    [HttpPost("otp/verify")]
    public async Task<ActionResult<ApiResponse<TokenPairDto>>> VerifyOtp(VerifyOtpCommand command, CancellationToken cancellationToken)
        => Ok(ApiResponse.Ok(await sender.Send(command, cancellationToken)));

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<TokenPairDto>>> Refresh(RefreshTokenCommand command, CancellationToken cancellationToken)
        => Ok(ApiResponse.Ok(await sender.Send(command, cancellationToken)));
}
