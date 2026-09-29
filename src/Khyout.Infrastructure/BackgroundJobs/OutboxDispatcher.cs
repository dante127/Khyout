using Khyout.Application.Abstractions;
using Khyout.Domain.Enums;
using Khyout.Infrastructure.Messaging.Telegram;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
                var delivered = await telegram.SendAsync(message.TargetRef, text, cancellationToken);

                if (delivered)
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
}
