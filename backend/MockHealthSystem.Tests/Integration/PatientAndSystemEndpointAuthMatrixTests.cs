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
/// Verifies that core domain routes (<c>/patients</c>, <c>/system</c>) honor the same auth modes as documented health checks,
/// using representative read endpoints (missing patient 404, system OData 200).
/// </summary>
public sealed class PatientAndSystemEndpointAuthMatrixTests : IClassFixture<IsolatedWebApplicationFactory>
{
    private const string MissingPatientPath = "/api/v1/patients/900000001";
    private const string SystemConditionsOdataPath = "/api/v1/system/conditions/odata";

    private readonly IsolatedWebApplicationFactory _factory;

    public PatientAndSystemEndpointAuthMatrixTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task NoneMode_AllowsPatientAndSystemWithoutCredentials()
    {
        await SetAuthModeAsync("None");
        var client = _factory.CreateClient();

        var patientResponse = await client.GetAsync(MissingPatientPath);
        await ApiErrorAssertions.AssertApiErrorAsync(patientResponse, HttpStatusCode.NotFound);

        var systemResponse = await client.GetAsync(SystemConditionsOdataPath);
        Assert.Equal(HttpStatusCode.OK, systemResponse.StatusCode);
    }

    [Fact]
    public async Task BearerMode_RequiresMatchingBearerToken_ForPatientAndSystem()
    {
        await SetAuthModeAsync("Bearer", bearerToken: "domain-secret");
        var client = _factory.CreateClient();

        var patientNoAuth = await client.GetAsync(MissingPatientPath);
        await ApiErrorAssertions.AssertApiErrorAsync(patientNoAuth, HttpStatusCode.Unauthorized);

        var systemNoAuth = await client.GetAsync(SystemConditionsOdataPath);
        await ApiErrorAssertions.AssertApiErrorAsync(systemNoAuth, HttpStatusCode.Unauthorized);

        using var wrongPatient = new HttpRequestMessage(HttpMethod.Get, MissingPatientPath);
        wrongPatient.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "wrong");
        await ApiErrorAssertions.AssertApiErrorAsync(await client.SendAsync(wrongPatient), HttpStatusCode.Unauthorized);

        using var wrongSystem = new HttpRequestMessage(HttpMethod.Get, SystemConditionsOdataPath);
        wrongSystem.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "wrong");
        await ApiErrorAssertions.AssertApiErrorAsync(await client.SendAsync(wrongSystem), HttpStatusCode.Unauthorized);

        using var okPatient = new HttpRequestMessage(HttpMethod.Get, MissingPatientPath);
        okPatient.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "domain-secret");
        await ApiErrorAssertions.AssertApiErrorAsync(await client.SendAsync(okPatient), HttpStatusCode.NotFound);

        using var okSystem = new HttpRequestMessage(HttpMethod.Get, SystemConditionsOdataPath);
        okSystem.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "domain-secret");
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(okSystem)).StatusCode);
    }

    [Fact]
    public async Task CcApiKeyMode_RequiresMatchingHeader_ForPatientAndSystem()
    {
        await SetAuthModeAsync("CCAPIKey", bearerToken: "cc-key");
        var client = _factory.CreateClient();

        await ApiErrorAssertions.AssertApiErrorAsync(await client.GetAsync(MissingPatientPath), HttpStatusCode.Unauthorized);
        await ApiErrorAssertions.AssertApiErrorAsync(await client.GetAsync(SystemConditionsOdataPath), HttpStatusCode.Unauthorized);

        using var wrongPatient = new HttpRequestMessage(HttpMethod.Get, MissingPatientPath);
        wrongPatient.Headers.Add("CCAPIKey", "wrong");
        await ApiErrorAssertions.AssertApiErrorAsync(await client.SendAsync(wrongPatient), HttpStatusCode.Unauthorized);

        using var wrongSystem = new HttpRequestMessage(HttpMethod.Get, SystemConditionsOdataPath);
        wrongSystem.Headers.Add("CCAPIKey", "wrong");
        await ApiErrorAssertions.AssertApiErrorAsync(await client.SendAsync(wrongSystem), HttpStatusCode.Unauthorized);

        using var okPatient = new HttpRequestMessage(HttpMethod.Get, MissingPatientPath);
        okPatient.Headers.Add("CCAPIKey", "cc-key");
        await ApiErrorAssertions.AssertApiErrorAsync(await client.SendAsync(okPatient), HttpStatusCode.NotFound);

        using var okSystem = new HttpRequestMessage(HttpMethod.Get, SystemConditionsOdataPath);
        okSystem.Headers.Add("CCAPIKey", "cc-key");
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(okSystem)).StatusCode);
    }

    [Fact]
    public async Task OAuthMode_RequiresIssuedAccessToken_ForPatientAndSystem()
    {
        const string accessToken = "oauth-domain-access";
        await SetAuthModeAsync("OAuth");
        await SeedAccessTokenAsync(accessToken, "client-x", "sub-x", DateTime.UtcNow.AddMinutes(30));

        var client = _factory.CreateClient();

        using var ccOnlyPatient = new HttpRequestMessage(HttpMethod.Get, MissingPatientPath);
        ccOnlyPatient.Headers.Add("CCAPIKey", accessToken);
        await ApiErrorAssertions.AssertApiErrorAsync(await client.SendAsync(ccOnlyPatient), HttpStatusCode.Unauthorized);

        using var missingAuthPatient = new HttpRequestMessage(HttpMethod.Get, MissingPatientPath);
        await ApiErrorAssertions.AssertApiErrorAsync(await client.SendAsync(missingAuthPatient), HttpStatusCode.Unauthorized);

        using var okPatient = new HttpRequestMessage(HttpMethod.Get, MissingPatientPath);
        okPatient.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        await ApiErrorAssertions.AssertApiErrorAsync(await client.SendAsync(okPatient), HttpStatusCode.NotFound);

        using var okSystem = new HttpRequestMessage(HttpMethod.Get, SystemConditionsOdataPath);
        okSystem.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(okSystem)).StatusCode);
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
