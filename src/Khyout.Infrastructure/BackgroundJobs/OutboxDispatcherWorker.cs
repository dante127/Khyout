using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Khyout.Infrastructure.BackgroundJobs;

/// <summary>Drains the outbox every 30 seconds (see <see cref="OutboxDispatcher"/>).</summary>
public sealed class OutboxDispatcherWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<OutboxDispatcherWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!BackgroundJobsOptions.IsEnabled(configuration))
        {
            logger.LogInformation("Outbox dispatcher worker is disabled by configuration.");
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                await scope.ServiceProvider.GetRequiredService<OutboxDispatcher>().ProcessAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox dispatch run failed; will retry on the next tick.");
            }
        }
    }
}
