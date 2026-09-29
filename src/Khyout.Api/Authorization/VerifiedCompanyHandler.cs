using Khyout.Application.Abstractions;
using Khyout.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace Khyout.Api.Authorization;

/// <summary>Requirement: the caller's company must have VerificationStatus = Verified.</summary>
public sealed class VerifiedCompanyRequirement : IAuthorizationRequirement
{
}

public sealed class VerifiedCompanyHandler(IAppDbContext db, ICurrentUser currentUser)
    : AuthorizationHandler<VerifiedCompanyRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        VerifiedCompanyRequirement requirement)
    {
        if (currentUser.CompanyId is not { } companyId)
        {
            return;
        }

        var company = await db.Companies.FindAsync(companyId);
        if (company is not null && company.VerificationStatus == VerificationStatus.Verified)
        {
            context.Succeed(requirement);
        }
    }
}
