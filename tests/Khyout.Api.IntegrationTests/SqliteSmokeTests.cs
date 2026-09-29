using FluentAssertions;
using Khyout.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Khyout.Api.IntegrationTests;

/// <summary>
/// Verifies the EF model materializes on SQLite (local-dev provider) with the
/// expected tables and seed data. Migrations themselves are PostgreSQL-only.
/// </summary>
public class SqliteSmokeTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"khyout-smoke-{Guid.NewGuid():N}.db");

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;

        var db = new AppDbContext(options);
        db.Database.EnsureCreated().Should().BeTrue();
        return db;
    }

    [Fact]
    public void Model_materializes_with_all_expected_tables()
    {
        using var db = CreateContext();

        var tables = new List<string>();
        using (var command = db.Database.GetDbConnection().CreateCommand())
        {
            command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table'";
            db.Database.OpenConnection();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                tables.Add(reader.GetString(0));
            }
        }

        tables.Should().Contain(new[]
        {
            "users",
            "refresh_tokens",
            "otp_codes",
            "companies",
            "categories",
            "products",
            "fabric_attributes",
            "fabric_composition",
            "product_images",
            "rfq_requests",
            "rfq_quotations",
            "sample_requests",
            "outbox_messages"
        });
    }

    [Fact]
    public void Starter_categories_are_seeded()
    {
        using var db = CreateContext();

        db.Categories.Should().HaveCount(10);
        db.Categories.Should().Contain(c => c.Slug == "cotton-fabrics" && c.NameAr == "أقمشة قطنية");
        db.Categories.Should().Contain(c => c.Slug == "cotton-yarn");
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
            // best effort cleanup; the temp folder is cleaned by the OS anyway
        }
    }
}
