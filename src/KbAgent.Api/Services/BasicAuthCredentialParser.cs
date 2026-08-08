using System.Text;

namespace KbAgent.Api.Services;

/// <summary>Pure parsing of an HTTP `Authorization: Basic base64(username:token)` header value.</summary>
public static class BasicAuthCredentialParser
{
    private const string SchemePrefix = "Basic ";

    public static bool TryParse(string? authorizationHeaderValue, out string username, out string token)
    {
        username = "";
        token = "";

        if (string.IsNullOrEmpty(authorizationHeaderValue) ||
            !authorizationHeaderValue.StartsWith(SchemePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string decoded;
        try
        {
            var base64 = authorizationHeaderValue[SchemePrefix.Length..].Trim();
            decoded = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
        }
        catch (FormatException)
        {
            return false;
        }

        var separatorIndex = decoded.IndexOf(':');
        if (separatorIndex < 0)
        {
            return false;
        }

        username = decoded[..separatorIndex];
        token = decoded[(separatorIndex + 1)..];
        return true;
    }
}
