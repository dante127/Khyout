using Khyout.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Khyout.Infrastructure.Persistence;

public class AppDbContext : DbContext
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
        base.OnModelCreating(modelBuilder);
    }
}
