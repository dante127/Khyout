namespace Khyout.Domain.Common;

/// <summary>
/// Raised when a domain invariant is violated. Carries a stable machine-readable
/// code so the API layer can map it to a specific client error later.
/// </summary>
public sealed class DomainRuleException : Exception
{
    public string Code { get; }

    public DomainRuleException(string code, string message) : base(message)
    {
        Code = code;
    }
}
