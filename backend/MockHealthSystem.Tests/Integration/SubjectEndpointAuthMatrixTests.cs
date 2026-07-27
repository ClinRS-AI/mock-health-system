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
/// Verifies GET /subjects/{id} and GET /studies/{studyUid}/subject-statuses/odata (representative
/// CC-mirrored Subject routes) honor all 4 auth modes, mirroring StudyEndpointAuthMatrixTests's
/// representative-route pattern.
/// </summary>
public sealed class SubjectEndpointAuthMatrixTests : IClassFixture<IsolatedWebApplicationFactory>
{
    private const string MissingSubjectPath = "/api/v1/subjects/900000001";
    private static readonly string MissingSubjectStatusesPath = $"/api/v1/studies/{Guid.NewGuid()}/subject-statuses/odata";

    private readonly IsolatedWebApplicationFactory _factory;

    public SubjectEndpointAuthMatrixTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task NoneMode_AllowsSubjectEndpointsWithoutCredentials()
    {
        await SetAuthModeAsync("None");
        var client = _factory.CreateClient();

        await ApiErrorAssertions.AssertApiErrorAsync(await client.GetAsync(MissingSubjectPath), HttpStatusCode.NotFound);
        await ApiErrorAssertions.AssertApiErrorAsync(await client.GetAsync(MissingSubjectStatusesPath), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task BearerMode_RequiresMatchingBearerToken()
    {
        await SetAuthModeAsync("Bearer", bearerToken: "subject-domain-secret");
        var client = _factory.CreateClient();

        foreach (var path in new[] { MissingSubjectPath, MissingSubjectStatusesPath })
        {
            var noAuth = await client.GetAsync(path);
            await ApiErrorAssertions.AssertApiErrorAsync(noAuth, HttpStatusCode.Unauthorized);

            using var wrong = new HttpRequestMessage(HttpMethod.Get, path);
            wrong.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "wrong");
            await ApiErrorAssertions.AssertApiErrorAsync(await client.SendAsync(wrong), HttpStatusCode.Unauthorized);

            using var ok = new HttpRequestMessage(HttpMethod.Get, path);
            ok.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "subject-domain-secret");
            await ApiErrorAssertions.AssertApiErrorAsync(await client.SendAsync(ok), HttpStatusCode.NotFound);
        }
    }

    [Fact]
    public async Task CcApiKeyMode_RequiresMatchingHeader()
    {
        await SetAuthModeAsync("CCAPIKey", bearerToken: "subject-cc-key");
        var client = _factory.CreateClient();

        foreach (var path in new[] { MissingSubjectPath, MissingSubjectStatusesPath })
        {
            await ApiErrorAssertions.AssertApiErrorAsync(await client.GetAsync(path), HttpStatusCode.Unauthorized);

            using var wrong = new HttpRequestMessage(HttpMethod.Get, path);
            wrong.Headers.Add("CCAPIKey", "wrong");
            await ApiErrorAssertions.AssertApiErrorAsync(await client.SendAsync(wrong), HttpStatusCode.Unauthorized);

            using var ok = new HttpRequestMessage(HttpMethod.Get, path);
            ok.Headers.Add("CCAPIKey", "subject-cc-key");
            await ApiErrorAssertions.AssertApiErrorAsync(await client.SendAsync(ok), HttpStatusCode.NotFound);
        }
    }

    [Fact]
    public async Task OAuthMode_RequiresIssuedAccessToken()
    {
        const string accessToken = "oauth-subject-access";
        await SetAuthModeAsync("OAuth");
        await SeedAccessTokenAsync(accessToken, "client-subject", "sub-subject", DateTime.UtcNow.AddMinutes(30));

        var client = _factory.CreateClient();

        foreach (var path in new[] { MissingSubjectPath, MissingSubjectStatusesPath })
        {
            using var missingAuth = new HttpRequestMessage(HttpMethod.Get, path);
            await ApiErrorAssertions.AssertApiErrorAsync(await client.SendAsync(missingAuth), HttpStatusCode.Unauthorized);

            using var ok = new HttpRequestMessage(HttpMethod.Get, path);
            ok.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            await ApiErrorAssertions.AssertApiErrorAsync(await client.SendAsync(ok), HttpStatusCode.NotFound);
        }
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
