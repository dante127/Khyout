using Khyout.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Khyout.Infrastructure.BackgroundJobs;

/// <summary>Deletes OTP codes older than 24 hours, hourly.</summary>
public sealed class OtpCleanupWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<OtpCleanupWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!BackgroundJobsOptions.IsEnabled(configuration))
        {
            logger.LogInformation("OTP cleanup worker is disabled by configuration.");
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
                var clock = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

                var cutoff = clock.UtcNow.AddHours(-24);
                var stale = await db.OtpCodes.Where(o => o.CreatedAt < cutoff).ToListAsync(stoppingToken);
                if (stale.Count > 0)
                {
                    db.OtpCodes.RemoveRange(stale);
                    await db.SaveChangesAsync(stoppingToken);
                    logger.LogInformation("Removed {Count} stale OTP codes.", stale.Count);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "OTP cleanup run failed; will retry on the next tick.");
            }
        }
    }
}
