using System.Text.Json;
using Khyout.Application.Abstractions;
using Khyout.Domain.Entities;
using Khyout.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Khyout.Infrastructure.BackgroundJobs;

/// <summary>
/// Expires quotations past their ValidUntil (mandatory price protection) and Open RFQs
/// past their ClosingDate. Enqueues outbox notifications in the same transaction.
/// Invoked by <see cref="ExpirationWorker"/> on a timer, and directly by tests.
/// </summary>
public sealed class ExpirationProcessor(IAppDbContext db, IDateTimeProvider clock, ILogger<ExpirationProcessor> logger)
{
    public async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;

        var expiredQuotations = await db.RfqQuotations
            .Where(q => q.Status == QuotationStatus.Submitted && q.ValidUntil <= now)
            .Take(200)
            .ToListAsync(cancellationToken);

        foreach (var quotation in expiredQuotations)
        {
            quotation.MarkExpired(now);
            db.OutboxMessages.Add(OutboxMessage.Create(
                OutboxMessageType.QuotationExpired,
                JsonSerializer.Serialize(new
                {
                    quotationId = quotation.Id,
                    rfqId = quotation.RfqRequestId,
                    supplierCompanyId = quotation.SupplierCompanyId
                }),
                quotation.SupplierCompanyId.ToString(),
                now));
        }

        var expiredRfqs = await db.RfqRequests
            .Where(r => r.Status == RfqStatus.Open && r.ClosingDate <= now)
            .Take(200)
            .ToListAsync(cancellationToken);

        foreach (var rfq in expiredRfqs)
        {
            rfq.MarkExpired(now);
        }

        if (expiredQuotations.Count > 0 || expiredRfqs.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Expired {QuotationCount} quotations and {RfqCount} RFQs.",
                expiredQuotations.Count,
                expiredRfqs.Count);
        }
    }
}
