using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MockHealthSystem.Api.RateLimiting;
using MockHealthSystem.Api.Services;
using MockHealthSystem.Infrastructure.Data;
using Xunit;

namespace MockHealthSystem.Tests.Integration;

public sealed class RateLimitEndpointTests : IClassFixture<IsolatedWebApplicationFactory>
{
    private readonly IsolatedWebApplicationFactory _factory;

    public RateLimitEndpointTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ApiEndpoint_Returns429_WhenPerSecondLimitExceeded()
    {
        await EnableRateLimitAsync(perSecond: 1);
        var client = _factory.CreateClient();

        var first = await client.GetAsync("/api/v1/health");
        var second = await client.GetAsync("/api/v1/health");

        Assert.NotEqual(HttpStatusCode.TooManyRequests, first.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, second.StatusCode);
    }

    [Fact]
    public async Task TooManyRequests_ResponseBody_IsValidApiErrorResponse()
    {
        await EnableRateLimitAsync(perSecond: 1);
        var client = _factory.CreateClient();

        await client.GetAsync("/api/v1/health");
        var resp = await client.GetAsync("/api/v1/health");

        await ApiErrorAssertions.AssertApiErrorAsync(
            resp,
            HttpStatusCode.TooManyRequests,
            titleContains: "Too Many Requests");
    }

    [Fact]
    public async Task TooManyRequests_Response_IncludesPositiveRetryAfterHeader()
    {
        await EnableRateLimitAsync(perSecond: 1);
        var client = _factory.CreateClient();

        await client.GetAsync("/api/v1/health");
        var resp = await client.GetAsync("/api/v1/health");

        Assert.Equal(HttpStatusCode.TooManyRequests, resp.StatusCode);
        Assert.True(resp.Headers.Contains("Retry-After"), "Retry-After header should be present on 429");
        var retryAfterRaw = resp.Headers.GetValues("Retry-After").First();
        Assert.True(int.TryParse(retryAfterRaw, out var retryAfter), "Retry-After should be an integer");
        Assert.True(retryAfter >= 1, "Retry-After should be at least 1");
    }

    [Fact]
    public async Task ApiEndpoint_NeverReturns429_WhenRateLimitDisabled()
    {
        await DisableRateLimitAsync();
        var client = _factory.CreateClient();

        var responses = new List<HttpResponseMessage>();
        for (var i = 0; i < 10; i++)
        {
            responses.Add(await client.GetAsync("/api/v1/health"));
        }

        Assert.All(responses, r => Assert.NotEqual(HttpStatusCode.TooManyRequests, r.StatusCode));
    }

    [Fact]
    public async Task PerMinuteLimitExhausted_RetryAfterIsGreaterThanOneSecond()
    {
        // High per-second so only the per-minute window fires.
        await EnableRateLimitAsync(perSecond: 100, perMinute: 2);
        var client = _factory.CreateClient();

        await client.GetAsync("/api/v1/health");
        await client.GetAsync("/api/v1/health");
        var resp = await client.GetAsync("/api/v1/health");

        Assert.Equal(HttpStatusCode.TooManyRequests, resp.StatusCode);
        var retryAfterRaw = resp.Headers.GetValues("Retry-After").First();
        Assert.True(int.TryParse(retryAfterRaw, out var retryAfter));
        Assert.True(retryAfter > 1, $"Retry-After should reflect minute window (>1s), got {retryAfter}");
    }

    [Fact]
    public async Task AdminEndpoints_NotThrottled_WhenApiRateLimitExhausted()
    {
        // Exhaust the API rate limit (per-second = 1) then confirm admin paths still succeed.
        await EnableRateLimitAsync(perSecond: 1);
        var client = _factory.CreateClient();

        await client.GetAsync("/api/v1/health");
        var throttled = await client.GetAsync("/api/v1/health");
        Assert.Equal(HttpStatusCode.TooManyRequests, throttled.StatusCode);

        // Admin paths use a separate counter — should not be blocked.
        var authSettingsResp = await client.GetAsync("/api/v1/auth-settings");
        Assert.NotEqual(HttpStatusCode.TooManyRequests, authSettingsResp.StatusCode);

        var monitoringResp = await client.GetAsync("/api/v1/monitoring/requests");
        Assert.NotEqual(HttpStatusCode.TooManyRequests, monitoringResp.StatusCode);

        var testDataResp = await client.GetAsync("/api/v1/test-data/patients/stats");
        Assert.NotEqual(HttpStatusCode.TooManyRequests, testDataResp.StatusCode);
    }

    [Fact]
    public async Task NonAdminEndpoint_Receives429_WhenApiLimitExhausted()
    {
        // Confirm /api/v1/health (not in admin set) is subject to the configured limit.
        await EnableRateLimitAsync(perSecond: 1);
        var client = _factory.CreateClient();

        await client.GetAsync("/api/v1/health"); // consume the 1-request window
        var resp = await client.GetAsync("/api/v1/health");

        Assert.Equal(HttpStatusCode.TooManyRequests, resp.StatusCode);
    }

    [Fact]
    public async Task DisablingRateLimit_AllowsRequestsThatWerePreviouslyBlocked()
    {
        await EnableRateLimitAsync(perSecond: 1);
        var client = _factory.CreateClient();

        await client.GetAsync("/api/v1/health"); // consume the 1-request limit
        var blockedResp = await client.GetAsync("/api/v1/health");
        Assert.Equal(HttpStatusCode.TooManyRequests, blockedResp.StatusCode);

        await DisableRateLimitAsync(); // disabling resets counters too

        var afterDisable = await client.GetAsync("/api/v1/health");
        Assert.NotEqual(HttpStatusCode.TooManyRequests, afterDisable.StatusCode);
    }

    [Fact]
    public async Task ReEnablingRateLimit_RestoresEnforcement()
    {
        await DisableRateLimitAsync();
        var client = _factory.CreateClient();

        // Disabled: burst succeeds
        for (var i = 0; i < 5; i++)
        {
            var r = await client.GetAsync("/api/v1/health");
            Assert.NotEqual(HttpStatusCode.TooManyRequests, r.StatusCode);
        }

        // Re-enable with limit=1: should throttle on second request
        await EnableRateLimitAsync(perSecond: 1);

        var first = await client.GetAsync("/api/v1/health");
        var second = await client.GetAsync("/api/v1/health");
        Assert.NotEqual(HttpStatusCode.TooManyRequests, first.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, second.StatusCode);
    }

    [Fact]
    public async Task CounterResetAfterSave_AllowsRequestsThatWereAtLimit()
    {
        await EnableRateLimitAsync(perSecond: 1);
        var client = _factory.CreateClient();

        await client.GetAsync("/api/v1/health"); // hit the limit
        var blocked = await client.GetAsync("/api/v1/health");
        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);

        // Save settings again (resets counters)
        await EnableRateLimitAsync(perSecond: 1);

        var afterReset = await client.GetAsync("/api/v1/health");
        Assert.NotEqual(HttpStatusCode.TooManyRequests, afterReset.StatusCode);
    }

    // ---- Helpers ----

    private async Task EnableRateLimitAsync(int perSecond, int perMinute = 300)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var settings = await db.AuthSettings.FirstAsync();
        settings.RateLimitEnabled = true;
        settings.RateLimitPerSecond = perSecond;
        settings.RateLimitPerMinute = perMinute;
        await db.SaveChangesAsync();

        var cacheService = scope.ServiceProvider.GetRequiredService<IAuthSettingsService>();
        await cacheService.InvalidateCacheAsync();

        var counterStore = scope.ServiceProvider.GetRequiredService<IRateLimitCounterStore>();
        counterStore.ResetAll();
    }

    private async Task DisableRateLimitAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var settings = await db.AuthSettings.FirstAsync();
        settings.RateLimitEnabled = false;
        await db.SaveChangesAsync();

        var cacheService = scope.ServiceProvider.GetRequiredService<IAuthSettingsService>();
        await cacheService.InvalidateCacheAsync();

        var counterStore = scope.ServiceProvider.GetRequiredService<IRateLimitCounterStore>();
        counterStore.ResetAll();
    }
}
