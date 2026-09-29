using Khyout.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Khyout.Infrastructure.BackgroundJobs;

/// <summary>Runs quote/RFQ expiry every minute (see <see cref="ExpirationProcessor"/>).</summary>
public sealed class ExpirationWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<ExpirationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!BackgroundJobsOptions.IsEnabled(configuration))
        {
            logger.LogInformation("Expiration worker is disabled by configuration.");
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                await scope.ServiceProvider.GetRequiredService<ExpirationProcessor>().ProcessAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Expiration run failed; will retry on the next tick.");
            }
        }
    }
}
