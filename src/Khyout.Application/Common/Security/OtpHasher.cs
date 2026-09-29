using System.Security.Cryptography;
using System.Text;

namespace Khyout.Application.Common.Security;

public static class OtpHasher
{
    /// <summary>Deterministic SHA-256 of (normalized phone + code); plaintext codes are never stored.</summary>
    public static string Hash(string normalizedPhoneNumber, string code)
    {
        var bytes = Encoding.UTF8.GetBytes(normalizedPhoneNumber + ":" + code);
        return Convert.ToHexString(SHA256.HashData(bytes));
    }
}
