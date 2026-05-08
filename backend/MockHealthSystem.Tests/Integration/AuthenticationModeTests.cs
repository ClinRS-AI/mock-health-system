using System.Net;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MockHealthSystem.Api.Services;
using MockHealthSystem.Infrastructure.Data;
using MockHealthSystem.Infrastructure.Data.Entities;
using Xunit;

namespace MockHealthSystem.Tests.Integration;

public sealed class AuthenticationModeTests : IClassFixture<MockHealthSystemWebApplicationFactory>
{
    private readonly MockHealthSystemWebApplicationFactory _factory;

    public AuthenticationModeTests(MockHealthSystemWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CcApiKeyMode_Succeeds_WithOnlyCcApiKeyHeader()
    {
        await ConfigureAuthSettingsAsync("CCAPIKey", bearerToken: "shared-secret");

        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/verify");
        request.Headers.Add("CCAPIKey", "shared-secret");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CcApiKeyMode_Fails_WhenHeaderMissingOrInvalid()
    {
        await ConfigureAuthSettingsAsync("CCAPIKey", bearerToken: "shared-secret");

        var client = _factory.CreateClient();

        var missingHeaderResponse = await client.GetAsync("/api/v1/auth/verify");
        await ApiErrorAssertions.AssertApiErrorAsync(missingHeaderResponse, HttpStatusCode.Unauthorized);

        using var invalidRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/verify");
        invalidRequest.Headers.Add("CCAPIKey", "wrong-secret");
        var invalidHeaderResponse = await client.SendAsync(invalidRequest);
        await ApiErrorAssertions.AssertApiErrorAsync(invalidHeaderResponse, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task BearerMode_RequiresAuthorizationBearerHeader()
    {
        await ConfigureAuthSettingsAsync("Bearer", bearerToken: "bearer-secret");

        var client = _factory.CreateClient();

        using var noAuthRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/verify");
        noAuthRequest.Headers.Add("CCAPIKey", "bearer-secret");
        var noAuthResponse = await client.SendAsync(noAuthRequest);
        await ApiErrorAssertions.AssertApiErrorAsync(noAuthResponse, HttpStatusCode.Unauthorized);

        using var authRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/verify");
        authRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "bearer-secret");
        var authResponse = await client.SendAsync(authRequest);
        Assert.Equal(HttpStatusCode.OK, authResponse.StatusCode);
    }

    [Fact]
    public async Task OAuthMode_RemainsBearerTokenBased()
    {
        await ConfigureAuthSettingsAsync("OAuth");
        await SeedAccessTokenAsync("oauth-access-token", "client-1", "subject-1", DateTime.UtcNow.AddMinutes(10));

        var client = _factory.CreateClient();

        using var missingAuthRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/verify");
        missingAuthRequest.Headers.Add("CCAPIKey", "oauth-access-token");
        var missingAuthResponse = await client.SendAsync(missingAuthRequest);
        await ApiErrorAssertions.AssertApiErrorAsync(missingAuthResponse, HttpStatusCode.Unauthorized);

        using var authRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/verify");
        authRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "oauth-access-token");
        var authResponse = await client.SendAsync(authRequest);
        Assert.Equal(HttpStatusCode.OK, authResponse.StatusCode);
    }

    private async Task ConfigureAuthSettingsAsync(string mode, string? bearerToken = null)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var settings = await db.AuthSettings.FirstOrDefaultAsync();
        if (settings is null)
        {
            settings = new AuthSettings
            {
                Id = 1,
                Mode = mode
            };
            db.AuthSettings.Add(settings);
        }

        settings.Mode = mode;
        settings.BearerToken = bearerToken;

        await db.SaveChangesAsync();

        var authSettingsService = scope.ServiceProvider.GetRequiredService<IAuthSettingsService>();
        await authSettingsService.InvalidateCacheAsync();
    }

    private async Task SeedAccessTokenAsync(string token, string clientId, string subject, DateTime expiresAtUtc)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var existingTokens = await db.AuthTokens.ToListAsync();
        if (existingTokens.Count > 0)
        {
            db.AuthTokens.RemoveRange(existingTokens);
        }

        db.AuthTokens.Add(new AuthToken
        {
            Token = token,
            TokenType = "access",
            ClientId = clientId,
            Subject = subject,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAtUtc,
            RevokedAt = null
        });

        await db.SaveChangesAsync();
    }
}
