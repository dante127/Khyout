using Khyout.Domain.Common;
using Khyout.Domain.Enums;

namespace Khyout.Domain.Entities;

/// <summary>Platform account. The phone number is the login identity.</summary>
public class User
{
    private User() { } // EF Core

    public Guid Id { get; private set; }
    /// <summary>E.164 normalized phone number, unique across the platform.</summary>
    public string PhoneNumber { get; private set; } = null!;

    public string FullName { get; private set; } = null!;

    public UserRole Role { get; private set; }

    /// <summary>Null for platform admins who are not attached to a company.</summary>
    public Guid? CompanyId { get; private set; }

    public Company? Company { get; private set; }
    public bool IsActive { get; private set; }

    /// <summary>Telegram chat id once the account is linked (null until then).</summary>
    public string? TelegramChatId { get; private set; }

    /// <summary>One-time code for the Telegram deep-link flow.</summary>
    public string? TelegramLinkCode { get; private set; }

    public DateTimeOffset? TelegramLinkCodeExpiresAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static User Create(string phoneNumber, string fullName, UserRole role, Guid? companyId, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new DomainRuleException("user_phone_required", "Phone number is required.");
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new DomainRuleException("user_name_required", "Full name is required.");
        }

        if (role != UserRole.Admin && companyId is null)
        {
            throw new DomainRuleException("user_company_required", "Buyer and Supplier users must belong to a company.");
        }

        return new User
        {
            Id = Guid.NewGuid(),
            PhoneNumber = phoneNumber.Trim(),
            FullName = fullName.Trim(),
            Role = role,
            CompanyId = companyId,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Rename(string fullName, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new DomainRuleException("user_name_required", "Full name is required.");
        }

        FullName = fullName.Trim();
        UpdatedAt = now;
    }

    public void Deactivate(DateTimeOffset now)
    {
        IsActive = false;
        UpdatedAt = now;
    }

    public void PromoteToAdmin(DateTimeOffset now)
    {
        Role = UserRole.Admin;
        UpdatedAt = now;
    }

    /// <summary>Stores a one-time code used to link a Telegram account (15-minute validity).</summary>
    public void BeginTelegramLink(string code, DateTimeOffset now, TimeSpan ttl)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainRuleException("telegram_code_required", "Link code is required.");
        }

        TelegramLinkCode = code;
        TelegramLinkCodeExpiresAt = now.Add(ttl);
        UpdatedAt = now;
    }

    public void SetTelegramChatId(string chatId, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(chatId))
        {
            throw new DomainRuleException("telegram_chat_required", "Chat id is required.");
        }

        TelegramChatId = chatId.Trim();
        TelegramLinkCode = null;
        TelegramLinkCodeExpiresAt = null;
        UpdatedAt = now;
    }

    public bool IsTelegramLinked => !string.IsNullOrWhiteSpace(TelegramChatId);
}
