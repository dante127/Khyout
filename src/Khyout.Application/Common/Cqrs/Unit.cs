namespace Khyout.Application.Common.Cqrs;

/// <summary>Response type for operations that produce no data.</summary>
public sealed record Unit
{
    public static readonly Unit Value = new();
}
