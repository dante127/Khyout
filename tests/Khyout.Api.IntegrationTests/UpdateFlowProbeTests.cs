using Khyout.Domain.Entities;
using Khyout.Domain.Enums;
using Khyout.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Khyout.Api.IntegrationTests;

/// <summary>Reproduces the product update flow at the DbContext level with SQL logging on failure.</summary>
public class UpdateFlowProbeTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"khyout-upd-{Guid.NewGuid():N}.db");

    [Fact]
    public async Task Product_update_flow_saves_cleanly()
    {
        var logPath = Path.Combine(Path.GetTempPath(), $"khyout-updlog-{Guid.NewGuid():N}.log");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .LogTo(line => File.AppendAllText(logPath, line + Environment.NewLine), LogLevel.Information)
            .EnableSensitiveDataLogging()
            .Options;

        var now = new DateTimeOffset(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);

        Guid productId;
        await using (var db = new AppDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();

            var company = Company.Create("Update Probe Co", CompanyType.Supplier, "aleppo", null, null, now);
            db.Companies.Add(company);
            await db.SaveChangesAsync();

            var category = await db.Categories.FirstAsync();

            var product = Product.Create(company.Id, category.Id, "Probe", null, 10m, UnitOfMeasure.Kg, now);
            product.UpdateDetails("Probe", null, 10m, UnitOfMeasure.Kg, null, null, now);
            product.SetAttributes(FabricAttributes.Create(product.Id, 160, 5, WeaveStructure.Plain, 150), now);
            product.ReplaceComposition(new[] { (FiberType.Cotton, 95m), (FiberType.Lycra, 5m) }, now);
            db.Products.Add(product);
            await db.SaveChangesAsync();

            productId = product.Id;
        }

        try
        {
            await using var db2 = new AppDbContext(options);
            var loaded = await db2.Products
                .Include(p => p.Attributes)
                .Include(p => p.Composition)
                .FirstAsync(p => p.Id == productId);

            loaded.UpdateDetails("Probe v2", null, 12m, UnitOfMeasure.Kg, null, null, now.AddDays(1));
            loaded.Attributes!.Update(170, 5, WeaveStructure.Plain, 150, null, null, null);
            loaded.ReplaceComposition(new[] { (FiberType.Cotton, 90m), (FiberType.Lycra, 10m) }, now.AddDays(1));

            // Same explicit-insert registration the UpdateProduct handler performs.
            db2.FabricCompositions.AddRange(loaded.Composition);

            loaded.ChangeStatus(ProductStatus.Active, now.AddDays(1));

            await db2.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            var sql = File.Exists(logPath) ? File.ReadAllText(logPath) : "(no SQL log captured)";
            Assert.Fail($"UPDATE FLOW FAILED: {ex.GetType().Name}: {ex.Message}\n\nSQL LOG:\n{sql}");
        }
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try
        {
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }
        }
        catch (IOException)
        {
        }
    }
}
