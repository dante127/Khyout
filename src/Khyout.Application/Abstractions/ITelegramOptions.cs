namespace Khyout.Application.Abstractions;

/// <summary>Telegram configuration surfaced to the application layer.</summary>
public interface ITelegramOptions
{
    bool Enabled { get; }

    /// <summary>Bot username used to build deep links (without the leading @).</summary>
    string? BotUsername { get; }

    string? WebhookSecret { get; }
}
