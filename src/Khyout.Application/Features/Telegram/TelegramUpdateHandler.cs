using System.Text.Json;
using Khyout.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Khyout.Application.Features.Telegram;

/// <summary>
/// Processes a Telegram webhook update: binds the chat id for a
/// "/start &lt;code&gt;" message whose code matches an outstanding link request.
/// </summary>
public sealed class TelegramUpdateHandler(
    IAppDbContext db,
    IDateTimeProvider clock,
    ITelegramSender telegram,
    ILogger<TelegramUpdateHandler> logger)
{
    public async Task HandleAsync(string rawBody, CancellationToken cancellationToken = default)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(rawBody);
        }
        catch (JsonException)
        {
            return;
        }

        using (document)
        {
            var root = document.RootElement;
            if (!root.TryGetProperty("message", out var message)
                || !message.TryGetProperty("chat", out var chat)
                || !chat.TryGetProperty("id", out var chatIdElement)
                || !message.TryGetProperty("text", out var textElement))
            {
                return;
            }

            var chatId = chatIdElement.ToString();
            var text = textElement.GetString() ?? string.Empty;

            if (!text.StartsWith("/start", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var code = text["/start".Length..].Trim();
            if (code.Length == 0)
            {
                return;
            }

            var now = clock.UtcNow;
            var user = await db.Users.FirstOrDefaultAsync(
                u => u.TelegramLinkCode == code && u.TelegramLinkCodeExpiresAt > now,
                cancellationToken);

            if (user is null)
            {
                logger.LogInformation("Telegram link attempt with an unknown or expired code.");
                return;
            }

            user.SetTelegramChatId(chatId, now);
            await db.SaveChangesAsync(cancellationToken);

            await telegram.SendAsync(
                chatId,
                "Your Telegram account is now linked to Khyout — you will receive RFQ and bid notifications here.",
                cancellationToken);
        }
    }
}
