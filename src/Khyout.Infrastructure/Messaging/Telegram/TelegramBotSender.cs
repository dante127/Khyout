using System.Net.Http.Json;
using Khyout.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Khyout.Infrastructure.Messaging.Telegram;

/// <summary>
/// Real Telegram Bot API sender. When Telegram is disabled or no bot token is
/// configured, falls back to the logging behavior so local development keeps working.
/// </summary>
public sealed class TelegramBotSender(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<TelegramBotSender> logger) : ITelegramSender
{
    public async Task<bool> SendAsync(string targetRef, string text, CancellationToken cancellationToken = default)
    {
        var configuration_enabled = bool.TryParse(configuration["Telegram:Enabled"], out var flag) && flag;
        var token = configuration["Telegram:BotToken"];

        if (!configuration_enabled || string.IsNullOrWhiteSpace(token))
        {
            logger.LogInformation("Telegram disabled — would send to {Target}: {Text}", targetRef, text);
            return true;
        }

        var response = await httpClient.PostAsJsonAsync(
            $"https://api.telegram.org/bot{token}/sendMessage",
            new { chat_id = targetRef, text },
            cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return true;
        }

        logger.LogWarning("Telegram sendMessage failed with {StatusCode}.", response.StatusCode);
        return false;
    }
}
