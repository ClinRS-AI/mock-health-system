using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MockHealthSystem.Infrastructure.Data;

namespace MockHealthSystem.Tests.Integration;

/// <summary>
/// Custom WebApplicationFactory that configures the API for integration tests (e.g. in-memory or test DB, no .env required).
/// </summary>
public sealed class MockHealthSystemWebApplicationFactory : WebApplicationFactory<Program>
{
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
                ["FRONTEND_URL"] = "http://localhost:5176"
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
}
