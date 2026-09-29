using Khyout.Application.Abstractions;
using Khyout.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Khyout.Infrastructure;

/// <summary>
/// Runs once at startup: if AdminBootstrap:PhoneNumber is configured and that user
/// exists, promotes them to Admin. Keeps admin provisioning out of migrations.
/// </summary>
public sealed class AdminBootstrapService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<AdminBootstrapService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var phone = configuration["AdminBootstrap:PhoneNumber"];
        if (string.IsNullOrWhiteSpace(phone))
        {
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

        var user = await db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phone.Trim(), stoppingToken);
        if (user is null)
        {
            logger.LogWarning("Admin bootstrap skipped: no user matches the configured phone number.");
            return;
        }

        if (user.Role != UserRole.Admin)
        {
            user.PromoteToAdmin(clock.UtcNow);
            await db.SaveChangesAsync(stoppingToken);
            logger.LogInformation("Admin bootstrap: promoted user {UserId} to Admin.", user.Id);
        }
    }
}
