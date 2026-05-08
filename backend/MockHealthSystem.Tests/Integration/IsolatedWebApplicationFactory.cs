using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MockHealthSystem.Infrastructure.Data;

namespace MockHealthSystem.Tests.Integration;

/// <summary>
/// Factory that overrides the DbContext to use a unique in-memory database name per instance.
/// This prevents test classes running in parallel from sharing the same in-memory database.
/// </summary>
public sealed class IsolatedWebApplicationFactory : WebApplicationFactory<Program>
{
    // One unique DB per factory instance → one per test class (IClassFixture creates one per class).
    private readonly string _dbName = $"TestDb_{Guid.NewGuid():N}";

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
            // Remove the DbContext registration that Program.cs added (hardcoded DB name).
            var toRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                            || d.ServiceType == typeof(AppDbContext))
                .ToList();
            foreach (var d in toRemove) services.Remove(d);

            // Re-register with an isolated database name.
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            // Seed minimal data required for auth.
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
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
