using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MockHealthSystem.Api.Services;
using MockHealthSystem.Infrastructure.Data;
using MockHealthSystem.Infrastructure.Data.Entities;
using Xunit;

namespace MockHealthSystem.Tests.Integration;

public sealed class HealthEndpointTests : IClassFixture<IsolatedWebApplicationFactory>
{
    private readonly IsolatedWebApplicationFactory _factory;

    public HealthEndpointTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetHealth_ReturnsOk_WithStatusMessage()
    {
        await ConfigureAuthModeNoneAsync();
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/health");

        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Mock Health System API is running", content);
    }

    private async Task ConfigureAuthModeNoneAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var settings = await db.AuthSettings.FirstOrDefaultAsync();
        if (settings is null)
        {
            settings = new AuthSettings
            {
                Id = 1
            };
            db.AuthSettings.Add(settings);
        }

        settings.Mode = "None";
        settings.BearerToken = null;
        await db.SaveChangesAsync();

        var authSettingsService = scope.ServiceProvider.GetRequiredService<IAuthSettingsService>();
        await authSettingsService.InvalidateCacheAsync();
    }
}
