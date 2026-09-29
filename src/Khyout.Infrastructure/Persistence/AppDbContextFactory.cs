using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Khyout.Infrastructure.Persistence;

/// <summary>
/// Design-time factory so <c>dotnet ef</c> can build the model without a running API.
/// The connection string here is never connected to; migrations only need the model.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=khyout_design;Username=design;Password=design-time-only")
            .Options;

        return new AppDbContext(options);
    }
}
