using System.Security.Cryptography;
using System.Text;

namespace KbAgent.Api.Services;

/// <summary>Generates and hashes API tokens. Only hashes are ever persisted — never the raw token.</summary>
public static class ApiTokenHasher
{
    public static (string RawToken, string TokenHash) GenerateToken()
    {
        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        return (rawToken, Hash(rawToken));
    }

    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    public static bool FixedTimeEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
}
