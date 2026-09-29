using Khyout.Application.Abstractions;

namespace Khyout.Infrastructure;

/// <summary>Default clock. Tests substitute a settable implementation.</summary>
public sealed class SystemClock : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
