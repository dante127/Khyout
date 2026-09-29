using FluentAssertions;
using Khyout.Domain.Entities;
using Khyout.Domain.Enums;
using Khyout.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Khyout.Api.IntegrationTests;

/// <summary>TEMPORARY probe: verifies decimal comparison/ordering translate on SQLite.</summary>
public class DecimalProbeTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"khyout-decprobe-{Guid.NewGuid():N}.db");

    [Fact]
    public async Task Decimal_translates_for_comparison_and_ordering()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;

        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var now = DateTimeOffset.UtcNow;
        var company = Company.Create("Probe Mills", CompanyType.Supplier, "aleppo", null, null, now);
        db.Companies.Add(company);
        await db.SaveChangesAsync();

        var category = await db.Categories.FirstAsync();
        db.Products.AddRange(
            Product.Create(company.Id, category.Id, "Small", null, 9.5m, UnitOfMeasure.Kg, now),
            Product.Create(company.Id, category.Id, "Medium", null, 50m, UnitOfMeasure.Kg, now),
            Product.Create(company.Id, category.Id, "Large", null, 120.75m, UnitOfMeasure.Kg, now));
        await db.SaveChangesAsync();

        var filtered = await db.Products
            .Where(p => p.Moq <= 100m)
            .OrderBy(p => p.Moq)
            .ToListAsync();

        filtered.Should().HaveCount(2);
        filtered[0].Moq.Should().Be(9.5m);
        filtered[1].Moq.Should().Be(50m);

        var highest = await db.Products.OrderByDescending(p => p.Moq).FirstAsync();
        highest.Moq.Should().Be(120.75m);
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
