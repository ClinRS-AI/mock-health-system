using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MockHealthSystem.Infrastructure.Data;

namespace MockHealthSystem.Tests.Integration;

/// <summary>
/// Custom WebApplicationFactory that configures the API for integration tests (e.g. in-memory or test DB, no .env required).
/// </summary>
public sealed class MockHealthSystemWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"mock-health-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "integration-test-key-min-32-characters-long",
                ["Jwt:Issuer"] = "MockHealthSystem.Api",
                ["Jwt:Audience"] = "MockHealthSystem.App",
                ["FRONTEND_URL"] = "http://localhost:5176",
                ["Testing:InMemoryDatabaseName"] = $"MockHealthSystemTests_{Guid.NewGuid():N}",
                ["Testing:UseSqlite"] = "true",
                ["Testing:SqliteConnectionString"] = $"Data Source={_dbPath}"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Build a temporary provider to seed minimal data required for auth.
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<AppDbContext>();

            db.Database.EnsureCreated();

            if (!db.AuthSettings.Any())
            {
                db.AuthSettings.Add(new Infrastructure.Data.Entities.AuthSettings
                {
                    Id = 1,
                    Mode = "None",
                    AccessTokenLifetimeMinutes = 60,
                    RefreshTokenLifetimeDays = 30
                });
                db.SaveChanges();
            }
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                if (File.Exists(_dbPath))
                {
                    File.Delete(_dbPath);
                }
            }
            catch
            {
                // Best-effort cleanup only.
            }
        }

        base.Dispose(disposing);
    }
}
