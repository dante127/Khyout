using Khyout.Domain.Entities;

namespace Khyout.Application.Features.Companies;

public sealed record CompanyDto(
    Guid Id,
    string Name,
    string Type,
    string City,
    string? Address,
    string? Bio,
    string VerificationStatus,
    DateTimeOffset CreatedAt)
{
    public static CompanyDto From(Company company) =>
        new(
            company.Id,
            company.Name,
            company.Type.ToString(),
            company.City,
            company.Address,
            company.Bio,
            company.VerificationStatus.ToString(),
            company.CreatedAt);
}
