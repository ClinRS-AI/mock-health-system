using System.Net;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MockHealthSystem.Api.Services;
using MockHealthSystem.Infrastructure.Data;
using MockHealthSystem.Infrastructure.Data.Entities;
using Xunit;

namespace MockHealthSystem.Tests.Integration;

/// <summary>
/// Verifies GET /studies/{id} (a representative CC-mirrored Study route) honors all 4 auth
/// modes, mirroring PatientAndSystemEndpointAuthMatrixTests's representative-route pattern.
/// StudyLookupController is intentionally out of scope here — it's admin-gated, not CC-auth-mode
/// gated (see StudyLookupControllerTests.LookupEndpoints_AreUnaffectedByActiveCcAuthMode).
/// </summary>
public sealed class StudyEndpointAuthMatrixTests : IClassFixture<IsolatedWebApplicationFactory>
{
    private const string MissingStudyPath = "/api/v1/studies/900000001";

    private readonly IsolatedWebApplicationFactory _factory;

    public StudyEndpointAuthMatrixTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task NoneMode_AllowsStudyEndpointWithoutCredentials()
    {
        await SetAuthModeAsync("None");
        var client = _factory.CreateClient();

        var resp = await client.GetAsync(MissingStudyPath);
        await ApiErrorAssertions.AssertApiErrorAsync(resp, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task BearerMode_RequiresMatchingBearerToken()
    {
        await SetAuthModeAsync("Bearer", bearerToken: "study-domain-secret");
        var client = _factory.CreateClient();

        var noAuth = await client.GetAsync(MissingStudyPath);
        await ApiErrorAssertions.AssertApiErrorAsync(noAuth, HttpStatusCode.Unauthorized);

        using var wrong = new HttpRequestMessage(HttpMethod.Get, MissingStudyPath);
        wrong.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "wrong");
        await ApiErrorAssertions.AssertApiErrorAsync(await client.SendAsync(wrong), HttpStatusCode.Unauthorized);

        using var ok = new HttpRequestMessage(HttpMethod.Get, MissingStudyPath);
        ok.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "study-domain-secret");
        await ApiErrorAssertions.AssertApiErrorAsync(await client.SendAsync(ok), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CcApiKeyMode_RequiresMatchingHeader()
    {
        await SetAuthModeAsync("CCAPIKey", bearerToken: "study-cc-key");
        var client = _factory.CreateClient();

        await ApiErrorAssertions.AssertApiErrorAsync(await client.GetAsync(MissingStudyPath), HttpStatusCode.Unauthorized);

        using var wrong = new HttpRequestMessage(HttpMethod.Get, MissingStudyPath);
        wrong.Headers.Add("CCAPIKey", "wrong");
        await ApiErrorAssertions.AssertApiErrorAsync(await client.SendAsync(wrong), HttpStatusCode.Unauthorized);

        using var ok = new HttpRequestMessage(HttpMethod.Get, MissingStudyPath);
        ok.Headers.Add("CCAPIKey", "study-cc-key");
        await ApiErrorAssertions.AssertApiErrorAsync(await client.SendAsync(ok), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task OAuthMode_RequiresIssuedAccessToken()
    {
        const string accessToken = "oauth-study-access";
        await SetAuthModeAsync("OAuth");
        await SeedAccessTokenAsync(accessToken, "client-study", "sub-study", DateTime.UtcNow.AddMinutes(30));

        var client = _factory.CreateClient();

        using var missingAuth = new HttpRequestMessage(HttpMethod.Get, MissingStudyPath);
        await ApiErrorAssertions.AssertApiErrorAsync(await client.SendAsync(missingAuth), HttpStatusCode.Unauthorized);

        using var ok = new HttpRequestMessage(HttpMethod.Get, MissingStudyPath);
        ok.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        await ApiErrorAssertions.AssertApiErrorAsync(await client.SendAsync(ok), HttpStatusCode.NotFound);
    }

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

        settings.Mode = mode;
        settings.BearerToken = bearerToken;

        await db.SaveChangesAsync();

        var cacheService = scope.ServiceProvider.GetRequiredService<IAuthSettingsService>();
        await cacheService.InvalidateCacheAsync();
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
            CreatedAt = IntegrationTestClock.UtcEpoch,
            ExpiresAt = expiresAtUtc,
            RevokedAt = null
        });

        await db.SaveChangesAsync();
    }
}
