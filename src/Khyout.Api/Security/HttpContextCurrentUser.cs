using System.Security.Claims;
using Khyout.Application.Abstractions;
using Khyout.Domain.Enums;

namespace Khyout.Api.Security;

/// <summary>Resolves the authenticated caller from JWT claims (sub / role / companyId).</summary>
public sealed class HttpContextCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid? UserId =>
        Guid.TryParse(Principal?.FindFirst("sub")?.Value, out var id) ? id : null;

    public UserRole? Role =>
        Enum.TryParse<UserRole>(Principal?.FindFirst("role")?.Value, out var role) ? role : null;

    public Guid? CompanyId =>
        Guid.TryParse(Principal?.FindFirst("companyId")?.Value, out var id) ? id : null;
}
