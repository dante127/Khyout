using FluentAssertions;
using Khyout.Domain.Entities;
using Khyout.Domain.Enums;
using Khyout.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Khyout.Api.IntegrationTests;

/// <summary>TEMPORARY probe: verifies DateTimeOffset comparison/ordering translate on SQLite.</summary>
public class DtProbeTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"khyout-dtprobe-{Guid.NewGuid():N}.db");

    [Fact]
    public async Task DateTimeOffset_translates_for_comparison_and_ordering()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;

        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var now = new DateTimeOffset(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);
        db.OtpCodes.AddRange(
            OtpCode.Create("+963900000001", OtpPurpose.Login, "h1", now.AddMinutes(-30)),
            OtpCode.Create("+963900000002", OtpPurpose.Login, "h2", now.AddMinutes(-10)),
            OtpCode.Create("+963900000003", OtpPurpose.Login, "h3", now.AddMinutes(-5)));
        await db.SaveChangesAsync();

        var cutoff = now.AddMinutes(-7);

        // Comparison in SQL:
        var expired = await db.OtpCodes
            .Where(o => o.ExpiresAt <= cutoff)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        expired.Should().NotBeEmpty();

        // Ordering in SQL:
        var latest = await db.OtpCodes
            .OrderByDescending(o => o.CreatedAt)
            .FirstAsync();

        latest.Should().NotBeNull();
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
