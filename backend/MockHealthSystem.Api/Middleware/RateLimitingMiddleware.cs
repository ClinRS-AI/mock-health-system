using System.Text.Json;
using MockHealthSystem.Api.Models;
using MockHealthSystem.Api.RateLimiting;
using MockHealthSystem.Api.Services;

namespace MockHealthSystem.Api.Middleware;

public sealed class RateLimitingMiddleware
{
    // Admin path prefixes: exempt from the configurable API rate limit.
    // These paths use a separate, fixed generous limit instead.
    private static readonly string[] AdminPaths =
    [
        "/api/v1/auth-settings",
        "/api/v1/monitoring",
        "/api/v1/test-data",
        "/api/v1/admin",
    ];

    private const int AdminPerSecond = 120;
    private const int AdminPerMinute = 5000;

    // Endpoints that verify a shared secret (admin key, OAuth client secret, refresh token,
    // SOAP report password) get a strict, non-configurable limit regardless of the
    // AdminPerSecond/AdminPerMinute allowance above and regardless of whether the
    // per-client RateLimitEnabled toggle is on — these are brute-force targets, not
    // general traffic, so throttling them can't be an opt-in.
    private static readonly (string Method, PathString Path)[] CredentialPaths =
    [
        (HttpMethods.Post, new PathString("/api/v1/admin/sessions")),
        (HttpMethods.Post, new PathString("/api/v1/auth/token")),
        (HttpMethods.Post, new PathString("/api/v1/auth/refresh")),
        (HttpMethods.Post, new PathString("/soap/report")),
    ];

    private const int CredentialPerSecond = 5;
    private const int CredentialPerMinute = 20;

    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly RequestDelegate _next;

    public RateLimitingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IAuthSettingsService authSettingsService,
        IRateLimitCounterStore counterStore)
    {
        // Use loopback as fallback — null RemoteIpAddress should not bypass rate limiting
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        if (IsCredentialPath(context.Request))
        {
            var (credentialAllowed, credentialRetryAfter) = counterStore.CheckAndIncrement(
                ip + ":credential", CredentialPerSecond, CredentialPerMinute);
            if (!credentialAllowed)
            {
                await WriteTooManyRequestsAsync(context, credentialRetryAfter);
                return;
            }

            await _next(context);
            return;
        }

        var path = context.Request.Path;
        bool isAdminPath = IsAdminPath(path);

        if (isAdminPath)
        {
            var (allowed, retryAfter) = counterStore.CheckAndIncrement(ip + ":admin", AdminPerSecond, AdminPerMinute);
            if (!allowed)
            {
                await WriteTooManyRequestsAsync(context, retryAfter);
                return;
            }
        }
        else
        {
            var settings = await authSettingsService.GetSettingsAsync(context.RequestAborted);
            if (settings.RateLimitEnabled)
            {
                // retryAfter = max(secondsUntilSecondReset, secondsUntilMinuteReset) — always the longer
            // window, so a client retrying after only the per-second reset won't immediately hit the
            // per-minute limit again.
                var (allowed, retryAfter) = counterStore.CheckAndIncrement(ip, settings.RateLimitPerSecond, settings.RateLimitPerMinute);
                if (!allowed)
                {
                    await WriteTooManyRequestsAsync(context, retryAfter);
                    return;
                }
            }
        }

        await _next(context);
    }

    private static bool IsAdminPath(PathString path)
    {
        foreach (var prefix in AdminPaths)
        {
            if (path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static bool IsCredentialPath(HttpRequest request)
    {
        foreach (var (method, path) in CredentialPaths)
        {
            if (string.Equals(request.Method, method, StringComparison.OrdinalIgnoreCase) &&
                request.Path.Equals(path, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static async Task WriteTooManyRequestsAsync(HttpContext context, int retryAfterSeconds)
    {
        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.Response.Headers.RetryAfter = retryAfterSeconds.ToString();
        context.Response.ContentType = "application/json";

        var body = new ApiErrorResponse
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too Many Requests",
            Detail = $"Rate limit exceeded. Retry after {retryAfterSeconds} seconds.",
            TraceId = context.TraceIdentifier
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOptions));
    }
}
