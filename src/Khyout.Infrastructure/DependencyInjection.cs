using Khyout.Application.Abstractions;
using Khyout.Infrastructure.BackgroundJobs;
using Khyout.Infrastructure.Identity;
using Khyout.Infrastructure.Media;
using Khyout.Infrastructure.Messaging.Sms;
using Khyout.Infrastructure.Messaging.Telegram;
using Khyout.Infrastructure.Persistence;
using Khyout.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Khyout.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers persistence and infrastructure services. Provider selection is
    /// configuration-driven: PostgreSQL by default (<c>ConnectionStrings:Postgres</c>),
    /// SQLite for local dev and tests (<c>Database:UseSqlite=true</c>). Background loops
    /// are disabled with <c>BackgroundJobs:Enabled=false</c> (used by the test suite).
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            // Resolve configuration from DI so the final (post-build) configuration is
            // used — important for WebApplicationFactory-based test hosts.
            var resolvedConfiguration = serviceProvider.GetRequiredService<IConfiguration>();
            var useSqlite = bool.TryParse(resolvedConfiguration["Database:UseSqlite"], out var sqliteFlag) && sqliteFlag;

            if (useSqlite)
            {
                var sqliteConnection = resolvedConfiguration.GetConnectionString("Sqlite")
                    ?? "Data Source=khyout.dev.db";
                options.UseSqlite(sqliteConnection);
            }
            else
            {
                var postgresConnection = resolvedConfiguration.GetConnectionString("Postgres");
                if (string.IsNullOrWhiteSpace(postgresConnection))
                {
                    throw new InvalidOperationException(
                        "ConnectionStrings:Postgres is required unless Database:UseSqlite is set to true.");
                }

                options.UseNpgsql(postgresConnection);
            }
        });

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddSingleton<IDateTimeProvider, SystemClock>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddSingleton<ISmsSender, LoggingSmsSender>();

        // Media pipeline: SkiaSharp transcode + local storage (root from Storage:MediaRoot).
        services.AddSingleton<IImageProcessor, SkiaSharpProcessor>();
        services.AddSingleton<IFileStorage>(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var environment = sp.GetRequiredService<IHostEnvironment>();
            var mediaRoot = configuration["Storage:MediaRoot"];
            if (string.IsNullOrWhiteSpace(mediaRoot))
            {
                mediaRoot = Path.Combine(environment.ContentRootPath, "media");
            }

            return new LocalFileStorage(mediaRoot);
        });

        // Telegram Bot API sender (falls back to logging when disabled/unconfigured).
        services.AddHttpClient<ITelegramSender, TelegramBotSender>();
        services.AddSingleton<ITelegramOptions, TelegramOptions>();

        // Processors are plain scoped services (tests invoke them directly);
        // the hosted workers wrap them in periodic timers.
        services.AddScoped<ExpirationProcessor>();
        services.AddScoped<OutboxDispatcher>();
        services.AddHostedService<ExpirationWorker>();
        services.AddHostedService<OutboxDispatcherWorker>();
        services.AddHostedService<OtpCleanupWorker>();
        services.AddHostedService<AdminBootstrapService>();

        return services;
    }
}
