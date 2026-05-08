using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MockHealthSystem.Api.Services;
using MockHealthSystem.Infrastructure.Data;
using MockHealthSystem.Infrastructure.Data.Entities;

namespace MockHealthSystem.Tests.Integration;

/// <summary>
/// Shared auth settings updates for integration tests to avoid duplicating EF + cache invalidation.
/// </summary>
internal static class IntegrationAuthSettingsHelper
{
    /// <summary>Forces auth mode to None and invalidates cache when an update was needed (including first-time insert).</summary>
    public static async Task EnsureNoneModeAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var cache = scope.ServiceProvider.GetRequiredService<IAuthSettingsService>();

        var settings = await db.AuthSettings.FirstOrDefaultAsync();
        if (settings is null)
        {
            db.AuthSettings.Add(new AuthSettings { Id = 1, Mode = "None" });
            await db.SaveChangesAsync();
            await cache.InvalidateCacheAsync();
            return;
        }

        if (settings.Mode == "None")
        {
            return;
        }

        settings.Mode = "None";
        await db.SaveChangesAsync();
        await cache.InvalidateCacheAsync();
    }
}
