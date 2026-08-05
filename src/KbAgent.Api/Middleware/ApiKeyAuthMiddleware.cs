using System.Security.Cryptography;
using System.Text;
using KbAgent.Api.Configuration;
using Microsoft.Extensions.Options;

namespace KbAgent.Api.Middleware;

/// <summary>Requires a matching X-Api-Key header when an API key is configured; a no-op when it isn't (local dev).</summary>
public sealed class ApiKeyAuthMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-Api-Key";

    public async Task InvokeAsync(HttpContext context, IOptions<ApiKeyOptions> options)
    {
        var configuredKey = options.Value.Key;

        if (string.IsNullOrEmpty(configuredKey))
        {
            await next(context);
            return;
        }

        var providedKey = context.Request.Headers[HeaderName].ToString();
        if (!FixedTimeEquals(providedKey, configuredKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Missing or invalid API key.");
            return;
        }

        await next(context);
    }

    private static bool FixedTimeEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
}
