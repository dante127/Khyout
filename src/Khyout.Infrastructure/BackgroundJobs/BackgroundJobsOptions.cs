using Microsoft.Extensions.Configuration;

namespace Khyout.Infrastructure.BackgroundJobs;

internal static class BackgroundJobsOptions
{
    /// <summary>Background loops are enabled unless explicitly configured off (tests set BackgroundJobs:Enabled=false).</summary>
    public static bool IsEnabled(IConfiguration configuration) =>
        !bool.TryParse(configuration["BackgroundJobs:Enabled"], out var enabled) || enabled;
}
