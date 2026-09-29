namespace Khyout.Application.Common;

/// <summary>Maps to HTTP 404.</summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
}

/// <summary>Maps to HTTP 403.</summary>
public sealed class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message)
    {
    }
}
