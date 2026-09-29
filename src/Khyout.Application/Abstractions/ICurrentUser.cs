using Khyout.Domain.Enums;

namespace Khyout.Application.Abstractions;

/// <summary>The authenticated caller, resolved from the HTTP context.</summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    Guid? UserId { get; }
    UserRole? Role { get; }
    Guid? CompanyId { get; }
}
