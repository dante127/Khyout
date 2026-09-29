using Khyout.Domain.Common;
using Khyout.Domain.Enums;

namespace Khyout.Domain.Entities;

/// <summary>A buyer workshop or a supplier mill/importer.</summary>
public class Company
{
    private Company() { } // EF Core

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public CompanyType Type { get; private set; }
    public string City { get; private set; } = null!;
    public string? Address { get; private set; }
    public string? Bio { get; private set; }
    public VerificationStatus VerificationStatus { get; private set; }
    public DateTimeOffset? VerifiedAt { get; private set; }
    public Guid? VerifiedByUserId { get; private set; }
    public string? LogoPath { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public ICollection<User> Users { get; private set; } = new List<User>();

    public static Company Create(string name, CompanyType type, string city, string? address, string? bio, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainRuleException("company_name_required", "Company name is required.");
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            throw new DomainRuleException("company_city_required", "Company city is required.");
        }

        return new Company
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Type = type,
            City = city.Trim(),
            Address = address,
            Bio = bio,
            VerificationStatus = VerificationStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateProfile(string name, string city, string? address, string? bio, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainRuleException("company_name_required", "Company name is required.");
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            throw new DomainRuleException("company_city_required", "Company city is required.");
        }

        Name = name.Trim();
        City = city.Trim();
        Address = address;
        Bio = bio;
        UpdatedAt = now;
    }

    public void Verify(Guid adminUserId, DateTimeOffset now)
    {
        EnsurePending();
        VerificationStatus = VerificationStatus.Verified;
        VerifiedAt = now;
        VerifiedByUserId = adminUserId;
        UpdatedAt = now;
    }

    public void Reject(DateTimeOffset now)
    {
        EnsurePending();
        VerificationStatus = VerificationStatus.Rejected;
        UpdatedAt = now;
    }

    private void EnsurePending()
    {
        if (VerificationStatus != VerificationStatus.Pending)
        {
            throw new DomainRuleException("company_not_pending", "Only companies in Pending status can change verification state.");
        }
    }
}
