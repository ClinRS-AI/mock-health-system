using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MockHealthSystem.Api.Services;
using MockHealthSystem.Infrastructure.Data;
using MockHealthSystem.Infrastructure.Data.Entities;
using Xunit;

namespace MockHealthSystem.Tests.Integration;

/// <summary>
/// StudyLookupController is Mock-Health-System admin configuration (research.md), gated by
/// IAdminRequestValidator like TestDataController's patients/lookup — not the CC auth mode.
/// </summary>
public sealed class StudyLookupControllerTests : IClassFixture<IsolatedWebApplicationFactory>
{
    private readonly IsolatedWebApplicationFactory _factory;

    public StudyLookupControllerTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetStudyCategories_Returns200_WhenNoAdminKeyConfigured()
    {
        await SeedCategoryAsync("Oncology");
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/v1/system/study-categories");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task GetStudySubcategories_Returns200()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/v1/system/study-subcategories");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task GetStudyTypes_Returns200()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/v1/system/study-types");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task GetStudyStatuses_Returns200()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/v1/system/study-statuses");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task GetStudyGroups_Returns200()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/v1/system/study-groups");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task GetStudyCategory_Returns404_WhenMissing()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/v1/system/study-categories/900000001");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task LookupEndpoints_AreUnaffectedByActiveCcAuthMode()
    {
        // These are admin-gated, not CC-auth-mode-gated: they must stay reachable (no admin key
        // configured in tests) even while a CC auth mode like Bearer is active and rejecting
        // CC-facing routes without credentials.
        await SetAuthModeAsync("Bearer", "some-secret");

        var client = _factory.CreateClient();

        var ccFacingResp = await client.GetAsync("/api/v1/studies/900000001");
        Assert.Equal(HttpStatusCode.Unauthorized, ccFacingResp.StatusCode);

        var lookupResp = await client.GetAsync("/api/v1/system/study-categories");
        Assert.Equal(HttpStatusCode.OK, lookupResp.StatusCode);

        // Reset for other tests sharing this factory instance.
        await SetAuthModeAsync("None", null);
    }

    private async Task SetAuthModeAsync(string mode, string? bearerToken)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var settings = await db.AuthSettings.FirstAsync();
        settings.Mode = mode;
        settings.BearerToken = bearerToken;
        await db.SaveChangesAsync();

        var cacheService = scope.ServiceProvider.GetRequiredService<IAuthSettingsService>();
        await cacheService.InvalidateCacheAsync();
    }

    private async Task SeedCategoryAsync(string name)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.StudyCategories.Add(new StudyCategory { Name = name });
        await db.SaveChangesAsync();
    }
}
