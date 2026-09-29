using Khyout.Application.Abstractions;
using Khyout.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Khyout.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<FabricAttributes> FabricAttributes => Set<FabricAttributes>();
    public DbSet<FabricComposition> FabricCompositions => Set<FabricComposition>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<RfqRequest> RfqRequests => Set<RfqRequest>();
    public DbSet<RfqQuotation> RfqQuotations => Set<RfqQuotation>();
    public DbSet<SampleRequest> SampleRequests => Set<SampleRequest>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // SQLite cannot translate DateTimeOffset ordering/comparison over its default
        // TEXT storage — store UTC ticks as INTEGER there instead. PostgreSQL keeps
        // native timestamptz columns (its migration is unaffected).
        if (Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
        {
            var converter = new UtcDateTimeOffsetConverter();
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTimeOffset) || property.ClrType == typeof(DateTimeOffset?))
                    {
                        property.SetValueConverter(converter);
                    }

                    // NUMERIC affinity so check constraints and comparisons see real numbers
                    // (EF's default TEXT storage for decimal breaks CHECK (value <= 100) style
                    // constraints and makes ordering lexicographic).
                    if (property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?))
                    {
                        property.SetColumnType("NUMERIC");
                    }
                }
            }
        }

        base.OnModelCreating(modelBuilder);
    }
}
