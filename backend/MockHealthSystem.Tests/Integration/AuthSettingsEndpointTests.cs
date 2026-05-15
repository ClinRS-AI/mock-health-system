using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MockHealthSystem.Infrastructure.Data;
using MockHealthSystem.Infrastructure.Data.Entities;
using Xunit;

namespace MockHealthSystem.Tests.Integration;

[Collection("EnvironmentMutating")]
public sealed class AuthSettingsEndpointTests : IClassFixture<IsolatedWebApplicationFactory>
{
    private readonly IsolatedWebApplicationFactory _factory;

    public AuthSettingsEndpointTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ---- GET /auth-settings ----

    [Fact]
    public async Task GetAuthSettings_Returns200_WhenNoAdminKeyConfigured()
    {
        // AUTH_SETTINGS_ADMIN_KEY is not set in tests → open access.
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/v1/auth-settings");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task GetAuthSettings_ReturnsCurrentMode()
    {
        await SetAuthModeAsync("Bearer", bearerToken: "get-test-token");
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/v1/auth-settings");
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<AuthSettingsDto>();

        Assert.Equal("Bearer", body!.Mode);
    }

    [Fact]
    public async Task GetAuthSettings_Returns403_WhenAdminKeyMissingOrWrong_IfConfigured()
    {
        using var _ = new EnvironmentVariableScope("AUTH_SETTINGS_ADMIN_KEY", "test-admin-key");
        var client = _factory.CreateClient();

        var missingResp = await client.GetAsync("/api/v1/auth-settings");
        Assert.Equal(HttpStatusCode.Forbidden, missingResp.StatusCode);

        using var wrongRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth-settings");
        wrongRequest.Headers.Add("X-Admin-Key", "wrong-key");
        var wrongResp = await client.SendAsync(wrongRequest);
        Assert.Equal(HttpStatusCode.Forbidden, wrongResp.StatusCode);
    }

    [Fact]
    public async Task GetAuthSettings_Returns200_WhenAdminSessionValid_IfConfigured()
    {
        using var _ = new EnvironmentVariableScope("AUTH_SETTINGS_ADMIN_KEY", "test-admin-key");
        var client = _factory.CreateClient();

        var mintResp = await client.PostAsJsonAsync("/api/v1/admin/sessions", new { adminKey = "test-admin-key" });
        mintResp.EnsureSuccessStatusCode();
        var mintBody = await mintResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var sessionToken = mintBody.GetProperty("accessToken").GetString()!;

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth-settings");
        request.Headers.Add("X-Admin-Session", sessionToken);

        var resp = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task GetAuthSettings_ReturnsHasAnyTokens_WhenTokensExist()
    {
        await SetAuthModeAsync("None");
        await SeedTokenAsync("has-any-token-test");
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/v1/auth-settings");
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<AuthSettingsDto>();

        Assert.True(body!.HasAnyTokens);
    }

    // ---- PUT /auth-settings ----

    [Fact]
    public async Task UpdateAuthSettings_Returns200_WhenModeIsValid()
    {
        var client = _factory.CreateClient();

        var resp = await client.PutAsJsonAsync("/api/v1/auth-settings", new
        {
            mode = "None"
        });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateAuthSettings_Returns400_WhenModeIsInvalid()
    {
        var client = _factory.CreateClient();

        var resp = await client.PutAsJsonAsync("/api/v1/auth-settings", new
        {
            mode = "InvalidMode"
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateAuthSettings_Returns403_WhenAdminKeyMissingOrWrong_IfConfigured()
    {
        using var _ = new EnvironmentVariableScope("AUTH_SETTINGS_ADMIN_KEY", "test-admin-key");
        var client = _factory.CreateClient();

        var missingResp = await client.PutAsJsonAsync("/api/v1/auth-settings", new { mode = "None" });
        Assert.Equal(HttpStatusCode.Forbidden, missingResp.StatusCode);

        using var wrongRequest = new HttpRequestMessage(HttpMethod.Put, "/api/v1/auth-settings")
        {
            Content = JsonContent.Create(new { mode = "None" })
        };
        wrongRequest.Headers.Add("X-Admin-Key", "wrong-key");
        var wrongResp = await client.SendAsync(wrongRequest);
        Assert.Equal(HttpStatusCode.Forbidden, wrongResp.StatusCode);
    }

    [Fact]
    public async Task UpdateAuthSettings_Returns200_WhenAdminSessionValid_IfConfigured()
    {
        using var _ = new EnvironmentVariableScope("AUTH_SETTINGS_ADMIN_KEY", "test-admin-key");
        var client = _factory.CreateClient();

        var mintResp = await client.PostAsJsonAsync("/api/v1/admin/sessions", new { adminKey = "test-admin-key" });
        mintResp.EnsureSuccessStatusCode();
        var mintBody = await mintResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var sessionToken = mintBody.GetProperty("accessToken").GetString()!;

        using var request = new HttpRequestMessage(HttpMethod.Put, "/api/v1/auth-settings")
        {
            Content = JsonContent.Create(new { mode = "None" })
        };
        request.Headers.Add("X-Admin-Session", sessionToken);

        var resp = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateAuthSettings_Returns400_WhenModeIsEmpty()
    {
        var client = _factory.CreateClient();

        var resp = await client.PutAsJsonAsync("/api/v1/auth-settings", new
        {
            mode = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateAuthSettings_UpdatesBearerToken()
    {
        var client = _factory.CreateClient();

        var resp = await client.PutAsJsonAsync("/api/v1/auth-settings", new
        {
            mode = "Bearer",
            bearerToken = "new-bearer-token-xyz"
        });

        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<AuthSettingsDto>();
        Assert.Equal("Bearer", body!.Mode);
        Assert.Equal("new-bearer-token-xyz", body.BearerToken);
    }

    [Fact]
    public async Task UpdateAuthSettings_UpdatesOAuthCredentials()
    {
        var client = _factory.CreateClient();

        var resp = await client.PutAsJsonAsync("/api/v1/auth-settings", new
        {
            mode = "OAuth",
            oAuthClientId = "my-client-id",
            oAuthClientSecret = "my-client-secret"
        });

        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<AuthSettingsDto>();
        Assert.Equal("OAuth", body!.Mode);
        Assert.Equal("my-client-id", body.OAuthClientId);
        Assert.Equal("my-client-secret", body.OAuthClientSecret);
    }

    [Fact]
    public async Task UpdateAuthSettings_ClearsTokens_WhenSwitchingToOAuth()
    {
        // Seed tokens that should be cleared on switch to OAuth.
        await SetAuthModeAsync("Bearer");
        await SeedTokenAsync("stale-token-for-oauth-switch");

        var client = _factory.CreateClient();
        var resp = await client.PutAsJsonAsync("/api/v1/auth-settings", new
        {
            mode = "OAuth",
            oAuthClientId = "cl",
            oAuthClientSecret = "sec"
        });

        resp.EnsureSuccessStatusCode();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tokenCount = await db.AuthTokens.CountAsync(t => t.Token == "stale-token-for-oauth-switch");
        Assert.Equal(0, tokenCount);
    }

    [Fact]
    public async Task UpdateAuthSettings_RevokesTokens_WhenSwitchingAwayFromOAuth()
    {
        await SetAuthModeAsync("OAuth");
        var uniqueToken = $"away-from-oauth-{Guid.NewGuid():N}";
        await SeedTokenAsync(uniqueToken);

        var client = _factory.CreateClient();
        var resp = await client.PutAsJsonAsync("/api/v1/auth-settings", new
        {
            mode = "None"
        });

        resp.EnsureSuccessStatusCode();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tokenCount = await db.AuthTokens.CountAsync(t => t.Token == uniqueToken);
        Assert.Equal(0, tokenCount);
    }

    [Fact]
    public async Task UpdateAuthSettings_DoesNotClearTokens_WhenStayingInOAuth()
    {
        await SetAuthModeAsync("OAuth");
        var uniqueToken = $"stay-in-oauth-{Guid.NewGuid():N}";
        await SeedTokenAsync(uniqueToken);

        var client = _factory.CreateClient();
        var resp = await client.PutAsJsonAsync("/api/v1/auth-settings", new
        {
            mode = "OAuth",
            oAuthClientId = "new-client",
            oAuthClientSecret = "new-secret"
        });

        resp.EnsureSuccessStatusCode();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tokenCount = await db.AuthTokens.CountAsync(t => t.Token == uniqueToken);
        Assert.Equal(1, tokenCount);
    }

    [Fact]
    public async Task UpdateAuthSettings_InvalidatesCache_SoSubsequentGetReflectsNewMode()
    {
        var client = _factory.CreateClient();

        await client.PutAsJsonAsync("/api/v1/auth-settings", new { mode = "None" });
        await client.PutAsJsonAsync("/api/v1/auth-settings", new { mode = "Bearer", bearerToken = "abc" });

        var getResp = await client.GetAsync("/api/v1/auth-settings");
        var body = await getResp.Content.ReadFromJsonAsync<AuthSettingsDto>();

        Assert.Equal("Bearer", body!.Mode);
    }

    [Fact]
    public async Task UpdateAuthSettings_AcceptsCCAPIKeyMode_AndNormalizesSpacing()
    {
        var client = _factory.CreateClient();

        var resp = await client.PutAsJsonAsync("/api/v1/auth-settings", new
        {
            mode = "  CCAPIKey  ",
            bearerToken = "ccapi-key-value"
        });

        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<AuthSettingsDto>();
        Assert.Equal("CCAPIKey", body!.Mode);
        Assert.Equal("ccapi-key-value", body.BearerToken);
    }

    [Fact]
    public async Task UpdateAuthSettings_PartialUpdate_DoesNotOverwriteOmittedFields()
    {
        var client = _factory.CreateClient();

        // First, set known values.
        var initial = await client.PutAsJsonAsync("/api/v1/auth-settings", new
        {
            mode = "Bearer",
            bearerToken = "initial-bearer",
            accessTokenLifetimeMinutes = 17,
            refreshTokenLifetimeDays = 5
        });
        initial.EnsureSuccessStatusCode();

        // Partial update only changes mode; lifetimes and bearer token must be retained.
        var partial = await client.PutAsJsonAsync("/api/v1/auth-settings", new
        {
            mode = "Bearer"
        });
        partial.EnsureSuccessStatusCode();

        var body = await partial.Content.ReadFromJsonAsync<AuthSettingsDto>();
        Assert.Equal("initial-bearer", body!.BearerToken);
        Assert.Equal(17, body.AccessTokenLifetimeMinutes);
        Assert.Equal(5, body.RefreshTokenLifetimeDays);
    }

    [Fact]
    public async Task UpdateAuthSettings_CreatesRow_WhenAuthSettingsTableEmpty()
    {
        // Remove the seeded AuthSettings row so the controller's "create" branch is exercised.
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.AuthSettings.RemoveRange(db.AuthSettings);
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        var resp = await client.PutAsJsonAsync("/api/v1/auth-settings", new
        {
            mode = "None"
        });

        resp.EnsureSuccessStatusCode();

        await using var scope2 = _factory.Services.CreateAsyncScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db2.AuthSettings.CountAsync());
    }

    // ---- Helpers ----

    private async Task SetAuthModeAsync(string mode, string? bearerToken = null)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var settings = await db.AuthSettings.FirstOrDefaultAsync();
        if (settings is null)
        {
            settings = new AuthSettings { Id = 1, Mode = mode };
            db.AuthSettings.Add(settings);
        }
        else
        {
            settings.Mode = mode;
            settings.BearerToken = bearerToken;
        }

        await db.SaveChangesAsync();

        var cacheService = scope.ServiceProvider.GetRequiredService<MockHealthSystem.Api.Services.IAuthSettingsService>();
        await cacheService.InvalidateCacheAsync();
    }

    private async Task SeedTokenAsync(string token)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.AuthTokens.Add(new AuthToken
        {
            Token = token,
            TokenType = "access",
            ClientId = "test",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        });

        await db.SaveChangesAsync();
    }

    private sealed class AuthSettingsDto
    {
        public string? Mode { get; set; }
        public string? BearerToken { get; set; }
        public string? OAuthClientId { get; set; }
        public string? OAuthClientSecret { get; set; }
        public int AccessTokenLifetimeMinutes { get; set; }
        public int RefreshTokenLifetimeDays { get; set; }
        public bool HasAnyTokens { get; set; }
    }
}
