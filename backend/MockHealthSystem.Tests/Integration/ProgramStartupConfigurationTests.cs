using System.Net;
using Microsoft.Extensions.DependencyInjection;
using MockHealthSystem.Infrastructure.Data;
using Xunit;

namespace MockHealthSystem.Tests.Integration;

/// <summary>
/// Integration tests focused on Program.cs startup wiring branches: the root health endpoint,
/// the seeded AuthSettings row, and the testing-host DI resolution path.
/// </summary>
public sealed class ProgramStartupConfigurationTests
{
    [Fact]
    public async Task Root_HealthEndpoint_Returns200_WithRunningMessage()
    {
        await using var factory = new IsolatedWebApplicationFactory();
        var client = factory.CreateClient();

        var resp = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("Mock Health System API", body);
    }

    [Fact]
    public async Task Startup_RegistersAuthSettingsTable_WithSeededRow_OnTestingHost()
    {
        await using var factory = new IsolatedWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.True(db.AuthSettings.Any());
    }

    [Fact]
    public async Task Startup_RegistersScopedAuthSettingsAndReportServices()
    {
        await using var factory = new IsolatedWebApplicationFactory();
        using var scope = factory.Services.CreateScope();

        var authService = scope.ServiceProvider
            .GetRequiredService<MockHealthSystem.Api.Services.IAuthSettingsService>();
        var reportService = scope.ServiceProvider
            .GetRequiredService<MockHealthSystem.Api.Services.IReportExecutionService>();
        var soapService = scope.ServiceProvider
            .GetRequiredService<MockHealthSystem.Api.Soap.IReportSoapService>();

        Assert.NotNull(authService);
        Assert.NotNull(reportService);
        Assert.NotNull(soapService);
    }
}
