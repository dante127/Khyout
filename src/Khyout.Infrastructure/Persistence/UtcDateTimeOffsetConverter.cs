using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Khyout.Infrastructure.Persistence;

/// <summary>
/// SQLite has no native DateTimeOffset support and cannot translate ordering/comparison
/// over its default TEXT storage. Storing UTC ticks as INTEGER keeps every query
/// provider-safe. Applied only for the SQLite provider; PostgreSQL keeps native
/// timestamptz columns (and its existing migration is unaffected).
/// </summary>
public sealed class UtcDateTimeOffsetConverter : ValueConverter<DateTimeOffset, long>
{
    public UtcDateTimeOffsetConverter()
        : base(
            value => value.UtcTicks,
            ticks => new DateTimeOffset(ticks, TimeSpan.Zero))
    {
    }
}
