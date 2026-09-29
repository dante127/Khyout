using Khyout.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Khyout.Infrastructure.Messaging.Sms;

/// <summary>
/// Development stand-in: logs the SMS (OTP codes appear here during local runs).
/// A real gateway/provider is plugged in behind the same abstraction later.
/// </summary>
public sealed class LoggingSmsSender(ILogger<LoggingSmsSender> logger) : ISmsSender
{
    public Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("SMS to {PhoneNumber}: {Message}", phoneNumber, message);
        return Task.CompletedTask;
    }
}
