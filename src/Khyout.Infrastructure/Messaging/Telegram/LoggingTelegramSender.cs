using Khyout.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Khyout.Infrastructure.Messaging.Telegram;

/// <summary>
/// Development stand-in: logs the notification and reports success (so the outbox
/// pipeline is exercised end-to-end). The real Telegram Bot integration arrives in Phase 4.
/// </summary>
public sealed class LoggingTelegramSender(ILogger<LoggingTelegramSender> logger) : ITelegramSender
{
    public Task<bool> SendAsync(string targetRef, string text, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Telegram notification to {Target}: {Text}", targetRef, text);
        return Task.FromResult(true);
    }
}
