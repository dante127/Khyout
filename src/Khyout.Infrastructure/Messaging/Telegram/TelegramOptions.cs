using Khyout.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace Khyout.Infrastructure.Messaging.Telegram;

public sealed class TelegramOptions(IConfiguration configuration) : ITelegramOptions
{
    public bool Enabled => bool.TryParse(configuration["Telegram:Enabled"], out var flag) && flag;

    public string? BotUsername => configuration["Telegram:BotUsername"];

    public string? WebhookSecret => configuration["Telegram:WebhookSecret"];
}
