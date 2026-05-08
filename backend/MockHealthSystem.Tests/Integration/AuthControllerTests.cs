using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MockHealthSystem.Infrastructure.Data;
using MockHealthSystem.Infrastructure.Data.Entities;
using Xunit;

namespace MockHealthSystem.Tests.Integration;

public sealed class AuthControllerTests : IClassFixture<IsolatedWebApplicationFactory>
{
    private readonly IsolatedWebApplicationFactory _factory;

    public AuthControllerTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ---- /auth/token ----

    [Fact]
    public async Task CreateToken_Returns400_WhenModeIsNone()
    {
        await SetAuthModeAsync("None");
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/auth/token", new
        {
            clientId = "any",
            clientSecret = "any"
        });

        await ApiErrorAssertions.AssertApiErrorAsync(resp, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateToken_Returns400_WhenModeIsBearer()
    {
        await SetAuthModeAsync("Bearer", bearerToken: "secret");
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/auth/token", new
        {
            clientId = "any",
            clientSecret = "any"
        });

        await ApiErrorAssertions.AssertApiErrorAsync(resp, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateToken_Returns400_WhenClientIdIsEmpty()
    {
        await SetAuthModeAsync("OAuth", clientId: "client1", clientSecret: "secret1");
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/auth/token", new
        {
            clientId = "",
            clientSecret = "secret1"
        });

        await ApiErrorAssertions.AssertApiErrorAsync(resp, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateToken_Returns401_WhenCredentialsAreWrong()
    {
        await SetAuthModeAsync("OAuth", clientId: "real-client", clientSecret: "real-secret");
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/auth/token", new
        {
            clientId = "real-client",
            clientSecret = "wrong-secret"
        });

        await ApiErrorAssertions.AssertApiErrorAsync(resp, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateToken_Returns200WithTokens_WhenCredentialsCorrect()
    {
        await SetAuthModeAsync("OAuth", clientId: "test-client", clientSecret: "test-secret");
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/auth/token", new
        {
            clientId = "test-client",
            clientSecret = "test-secret"
        });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<TokenResponseDto>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(body.RefreshToken));
        Assert.Equal("Bearer", body.TokenType);
        Assert.True(body.ExpiresIn > 0);
    }

    [Fact]
    public async Task CreateToken_PersistsTokensInDatabase()
    {
        await SetAuthModeAsync("OAuth", clientId: "persist-client", clientSecret: "persist-secret");
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/auth/token", new
        {
            clientId = "persist-client",
            clientSecret = "persist-secret"
        });

        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<TokenResponseDto>();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var token = await db.AuthTokens.FirstOrDefaultAsync(t => t.Token == body!.AccessToken);

        Assert.NotNull(token);
        Assert.Equal("access", token!.TokenType);
    }

    // ---- /auth/refresh ----

    [Fact]
    public async Task RefreshToken_Returns400_WhenModeIsNone()
    {
        await SetAuthModeAsync("None");
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            refreshToken = "any-token"
        });

        await ApiErrorAssertions.AssertApiErrorAsync(resp, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RefreshToken_Returns400_WhenRefreshTokenIsEmpty()
    {
        await SetAuthModeAsync("OAuth", clientId: "c", clientSecret: "s");
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            refreshToken = ""
        });

        await ApiErrorAssertions.AssertApiErrorAsync(resp, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RefreshToken_Returns401_WhenTokenDoesNotExist()
    {
        await SetAuthModeAsync("OAuth", clientId: "c", clientSecret: "s");
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            refreshToken = "nonexistent-refresh-token-12345"
        });

        await ApiErrorAssertions.AssertApiErrorAsync(resp, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_Returns401_WhenTokenIsExpired()
    {
        await SetAuthModeAsync("OAuth", clientId: "exp-client", clientSecret: "exp-secret");
        await SeedRefreshTokenAsync("expired-refresh-tok", "exp-client", DateTime.UtcNow.AddHours(-1));
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            refreshToken = "expired-refresh-tok"
        });

        await ApiErrorAssertions.AssertApiErrorAsync(resp, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_Returns200WithNewAccessToken_WhenRefreshTokenIsValid()
    {
        await SetAuthModeAsync("OAuth", clientId: "valid-client", clientSecret: "valid-secret");
        var validToken = $"valid-refresh-{Guid.NewGuid():N}";
        await SeedRefreshTokenAsync(validToken, "valid-client", DateTime.UtcNow.AddDays(30));
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            refreshToken = validToken
        });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<TokenResponseDto>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.AccessToken));
        Assert.Equal(validToken, body.RefreshToken);
    }

    // ---- /auth/verify ----

    [Fact]
    public async Task Verify_Returns200_InNoneMode()
    {
        await SetAuthModeAsync("None");
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/v1/auth/verify");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task Verify_Returns401_WhenBearerModeAndNoToken()
    {
        await SetAuthModeAsync("Bearer", bearerToken: "my-token");
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/v1/auth/verify");

        await ApiErrorAssertions.AssertApiErrorAsync(resp, HttpStatusCode.Unauthorized);
    }

    // ---- Helpers ----

    private async Task SetAuthModeAsync(string mode, string? bearerToken = null,
        string? clientId = null, string? clientSecret = null)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var settings = await db.AuthSettings.FirstOrDefaultAsync();
        if (settings is null)
        {
            settings = new AuthSettings { Id = 1, Mode = mode };
            db.AuthSettings.Add(settings);
        }

        settings.Mode = mode;
        settings.BearerToken = bearerToken;
        settings.OAuthClientId = clientId;
        settings.OAuthClientSecret = clientSecret;
        settings.AccessTokenLifetimeMinutes = 60;
        settings.RefreshTokenLifetimeDays = 30;

        await db.SaveChangesAsync();

        // Invalidate the service cache so the next request picks up the new settings.
        var cacheService = scope.ServiceProvider.GetRequiredService<MockHealthSystem.Api.Services.IAuthSettingsService>();
        await cacheService.InvalidateCacheAsync();
    }

    private async Task SeedRefreshTokenAsync(string token, string clientId, DateTime expiresAt)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.AuthTokens.Add(new AuthToken
        {
            Token = token,
            TokenType = "refresh",
            ClientId = clientId,
            Subject = null,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
            RevokedAt = null
        });

        await db.SaveChangesAsync();
    }

    private sealed class TokenResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string TokenType { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
    }
}
