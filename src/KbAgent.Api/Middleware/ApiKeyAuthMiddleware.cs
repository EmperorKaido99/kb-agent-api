using KbAgent.Api.Services;

namespace KbAgent.Api.Middleware;

/// <summary>
/// Requires valid `Authorization: Basic base64(username:token)` credentials matching a stored user. If no users
/// are configured (empty store), auth is a no-op (local dev convenience). Create users via
/// `dotnet run -- create-user &lt;username&gt;`.
/// </summary>
public sealed class ApiKeyAuthMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IApiUserStore userStore)
    {
        var users = await userStore.LoadAsync(context.RequestAborted);
        if (users.Count == 0)
        {
            await next(context);
            return;
        }

        var authorized =
            BasicAuthCredentialParser.TryParse(context.Request.Headers.Authorization, out var username, out var token) &&
            users.TryGetValue(username, out var expectedHash) &&
            ApiTokenHasher.FixedTimeEquals(ApiTokenHasher.Hash(token), expectedHash);

        if (!authorized)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers.WWWAuthenticate = "Basic realm=\"KbAgent.Api\"";
            await context.Response.WriteAsync("Missing or invalid credentials.");
            return;
        }

        await next(context);
    }
}
