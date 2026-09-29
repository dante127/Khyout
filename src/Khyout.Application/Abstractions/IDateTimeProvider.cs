namespace Khyout.Application.Abstractions;

/// <summary>Clock abstraction so time-dependent logic is testable.</summary>
public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
