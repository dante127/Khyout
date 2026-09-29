using FluentValidation.Results;

namespace Khyout.Application.Common.Cqrs;

/// <summary>Thrown when FluentValidation rejects a request. Maps to HTTP 400.</summary>
public sealed class RequestValidationException : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public RequestValidationException(IEnumerable<ValidationFailure> failures)
        : base("One or more validation errors occurred.")
    {
        Errors = failures
            .GroupBy(f => f.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).ToArray());
    }
}
