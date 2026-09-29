using Khyout.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Khyout.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the database context. Provider selection is configuration-driven:
    /// <list type="bullet">
    ///   <item>Default: PostgreSQL — <c>ConnectionStrings:Postgres</c>.</item>
    ///   <item>Local development: <c>Database:UseSqlite=true</c> (optional
    ///   <c>ConnectionStrings:Sqlite</c>, default <c>Data Source=khyout.dev.db</c>).
    ///   SQLite is for development only and uses <c>EnsureCreated()</c>, not migrations.</item>
    /// </list>
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var useSqlite = bool.TryParse(configuration["Database:UseSqlite"], out var sqliteFlag) && sqliteFlag;

        services.AddDbContext<AppDbContext>(options =>
        {
            if (useSqlite)
            {
                var sqliteConnection = configuration.GetConnectionString("Sqlite")
                    ?? "Data Source=khyout.dev.db";
                options.UseSqlite(sqliteConnection);
            }
            else
            {
                var postgresConnection = configuration.GetConnectionString("Postgres");
                if (string.IsNullOrWhiteSpace(postgresConnection))
                {
                    throw new InvalidOperationException(
                        "ConnectionStrings:Postgres is required unless Database:UseSqlite is set to true.");
                }

                options.UseNpgsql(postgresConnection);
            }
        });

        return services;
    }
}
