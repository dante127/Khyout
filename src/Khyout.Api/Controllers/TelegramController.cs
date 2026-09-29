using Khyout.Api.Contracts;
using Khyout.Application.Abstractions;
using Khyout.Application.Common.Cqrs;
using Khyout.Application.Features.Telegram;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Khyout.Api.Controllers;

[ApiController]
[Route("api/v1/telegram")]
public sealed class TelegramController(
    ISender sender,
    TelegramUpdateHandler updateHandler,
    ITelegramOptions telegramOptions) : ControllerBase
{
    /// <summary>Starts the account-linking flow; returns the deep link to open in Telegram.</summary>
    [Authorize]
    [HttpPost("link")]
    public async Task<ActionResult<ApiResponse<TelegramLinkDto>>> StartLink(CancellationToken cancellationToken)
        => Ok(ApiResponse.Ok(await sender.Send(new StartTelegramLinkCommand(), cancellationToken)));

    /// <summary>
    /// Telegram webhook target. The secret path segment must match Telegram:WebhookSecret;
    /// anything else is a 404. Always acks with 200 so Telegram does not retry forever.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("webhook/{secret}")]
    public async Task<IActionResult> Webhook(string secret, CancellationToken cancellationToken)
    {
        var expected = telegramOptions.WebhookSecret;
        if (string.IsNullOrWhiteSpace(expected) || !string.Equals(secret, expected, StringComparison.Ordinal))
        {
            return NotFound();
        }

        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(cancellationToken);
        await updateHandler.HandleAsync(body, cancellationToken);
        return Ok();
    }
}
