using Khyout.Application.Abstractions;
using Khyout.Domain.Common;
using Khyout.Domain.Entities;
using Khyout.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Khyout.Application.Common.Security;

/// <summary>Verifies an outstanding OTP for a phone number and consumes it.</summary>
public sealed class OtpVerifier(IAppDbContext db, IDateTimeProvider clock)
{
    public async Task VerifyAndConsumeAsync(
        string phoneNumber,
        OtpPurpose purpose,
        string code,
        CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        var normalized = NormalizePhone(phoneNumber);
        var hash = OtpHasher.Hash(normalized, code.Trim());

        var otp = await db.OtpCodes
            .Where(o => o.PhoneNumber == normalized && o.Purpose == purpose && o.ConsumedAt == null)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new DomainRuleException("otp_not_found", "No active code for this phone number. Request a new one.");

        if (otp.ExpiresAt <= now)
        {
            throw new DomainRuleException("otp_expired", "The code has expired. Request a new one.");
        }

        if (otp.AttemptsMade >= OtpCode.MaxAttempts)
        {
            throw new DomainRuleException("otp_attempts_exceeded", "Too many attempts. Request a new code.");
        }

        if (otp.CodeHash != hash)
        {
            otp.RegisterFailedAttempt();
            await db.SaveChangesAsync(cancellationToken);
            throw new DomainRuleException("otp_invalid", "The code is incorrect.");
        }

        otp.MarkConsumed(now);
        await db.SaveChangesAsync(cancellationToken);
    }

    public static string NormalizePhone(string phoneNumber) =>
        phoneNumber.Trim().Replace(" ", string.Empty);
}
