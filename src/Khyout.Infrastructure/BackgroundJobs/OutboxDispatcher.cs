using Khyout.Application.Abstractions;
using Khyout.Domain.Entities;
using Khyout.Domain.Enums;
using Khyout.Infrastructure.Messaging.Telegram;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Khyout.Infrastructure.BackgroundJobs;

/// <summary>
/// Drains the outbox: sends pending messages via the Telegram abstraction with
/// exponential backoff and dead-letters after <see cref="Domain.Entities.OutboxMessage.MaxAttempts"/>.
/// Invoked by <see cref="OutboxDispatcherWorker"/> on a timer, and directly by tests.
/// </summary>
public sealed class OutboxDispatcher(
    IAppDbContext db,
    IDateTimeProvider clock,
    ITelegramSender telegram,
    ILogger<OutboxDispatcher> logger)
{
    private const int BatchSize = 20;

    public async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;

        var pending = await db.OutboxMessages
            .Where(m => m.Status == OutboxMessageStatus.Pending && m.NextAttemptAt <= now)
            .OrderBy(m => m.NextAttemptAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            return;
        }

        var sentCount = 0;
        foreach (var message in pending)
        {
            try
            {
                var text = OutboxMessageRenderer.Render(message.Type, message.Payload);
                var recipients = await ResolveRecipientsAsync(message, cancellationToken);

                if (recipients.Count == 0)
                {
                    // Nobody has linked Telegram for this audience: nothing to deliver —
                    // mark sent so the queue does not grow unboundedly.
                    logger.LogInformation("Outbox message {MessageId}: no Telegram recipients.", message.Id);
                    message.MarkSent(clock.UtcNow);
                    sentCount++;
                    continue;
                }

                var allDelivered = true;
                foreach (var chatId in recipients)
                {
                    if (!await telegram.SendAsync(chatId, text, cancellationToken))
                    {
                        allDelivered = false;
                    }
                }

                if (allDelivered)
                {
                    message.MarkSent(clock.UtcNow);
                    sentCount++;
                }
                else
                {
                    message.RegisterFailure("Telegram delivery failed.", clock.UtcNow.Add(BackoffFor(message.Attempts)));
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Outbox dispatch failed for message {MessageId}.", message.Id);
                message.RegisterFailure(ex.Message, clock.UtcNow.Add(BackoffFor(message.Attempts)));
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogDebug("Outbox dispatch batch complete: {SentCount}/{TotalCount} sent.", sentCount, pending.Count);
    }

    private static TimeSpan BackoffFor(short attempts) =>
        TimeSpan.FromSeconds(Math.Min(30 * Math.Pow(2, attempts), 1800));

    /// <summary>
    /// Resolves Telegram chat ids for a message: RfqCreated targets verified suppliers with
    /// active products in the RFQ's category; other types target the company in the payload.
    /// </summary>
    private async Task<List<string>> ResolveRecipientsAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        JsonElement payload;
        try
        {
            payload = JsonDocument.Parse(message.Payload).RootElement;
        }
        catch (JsonException)
        {
            return new List<string>();
        }

        string? Get(string name) =>
            payload.ValueKind == JsonValueKind.Object && payload.TryGetProperty(name, out var value)
                ? value.GetString()
                : null;

        if (message.Type == OutboxMessageType.RfqCreated)
        {
            if (!Guid.TryParse(Get("categoryId"), out var categoryId))
            {
                return new List<string>();
            }

            return await db.Users
                .Where(u => u.TelegramChatId != null
                            && u.CompanyId != null
                            && u.Company!.VerificationStatus == VerificationStatus.Verified
                            && u.Company.Type == CompanyType.Supplier
                            && db.Products.Any(p => p.SupplierCompanyId == u.CompanyId
                                                    && p.CategoryId == categoryId
                                                    && p.Status == ProductStatus.Active))
                .Select(u => u.TelegramChatId!)
                .Distinct()
                .ToListAsync(cancellationToken);
        }

        var companyIdRaw = Get("companyId") ?? Get("supplierCompanyId") ?? Get("buyerCompanyId");
        if (!Guid.TryParse(companyIdRaw, out var companyId))
        {
            return new List<string>();
        }

        return await db.Users
            .Where(u => u.CompanyId == companyId && u.TelegramChatId != null)
            .Select(u => u.TelegramChatId!)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
