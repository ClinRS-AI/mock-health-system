using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MockHealthSystem.Api.Services;
using MockHealthSystem.Infrastructure.Data;
using MockHealthSystem.Infrastructure.Data.Entities;
using Xunit;

namespace MockHealthSystem.Tests.Unit;

public sealed class AuthSettingsServiceTests
{
    private static AppDbContext CreateDb(string name)
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new AppDbContext(opts);
    }

    private static IMemoryCache CreateCache() =>
        new MemoryCache(new MemoryCacheOptions());

    // ---- GetSettingsAsync: DB miss returns default ----

    [Fact]
    public async Task GetSettingsAsync_ReturnsDefault_WhenNoDatabaseRow()
    {
        await using var db = CreateDb(nameof(GetSettingsAsync_ReturnsDefault_WhenNoDatabaseRow));
        using var cache = CreateCache();
        var svc = new AuthSettingsService(db, cache);

        var result = await svc.GetSettingsAsync();

        Assert.Equal("None", result.Mode);
        Assert.Equal(1, result.Id);
    }

    // ---- GetSettingsAsync: DB hit returns stored row ----

    [Fact]
    public async Task GetSettingsAsync_ReturnsStoredSettings_WhenRowExists()
    {
        await using var db = CreateDb(nameof(GetSettingsAsync_ReturnsStoredSettings_WhenRowExists));
        db.AuthSettings.Add(new AuthSettings
        {
            Id = 1,
            Mode = "Bearer",
            BearerToken = "my-secret"
        });
        await db.SaveChangesAsync();

        using var cache = CreateCache();
        var svc = new AuthSettingsService(db, cache);

        var result = await svc.GetSettingsAsync();

        Assert.Equal("Bearer", result.Mode);
        Assert.Equal("my-secret", result.BearerToken);
    }

    // ---- GetSettingsAsync: result is cached ----

    [Fact]
    public async Task GetSettingsAsync_ReturnsCachedValue_OnSubsequentCalls()
    {
        await using var db = CreateDb(nameof(GetSettingsAsync_ReturnsCachedValue_OnSubsequentCalls));
        db.AuthSettings.Add(new AuthSettings { Id = 1, Mode = "Bearer", BearerToken = "initial" });
        await db.SaveChangesAsync();

        using var cache = CreateCache();
        var svc = new AuthSettingsService(db, cache);

        // First call populates the cache.
        var first = await svc.GetSettingsAsync();

        // Mutate DB directly, bypassing the service.
        var row = await db.AuthSettings.FirstAsync();
        row.BearerToken = "changed-in-db";
        await db.SaveChangesAsync();

        // Second call must return the cached (original) value.
        var second = await svc.GetSettingsAsync();

        Assert.Equal("initial", first.BearerToken);
        Assert.Equal("initial", second.BearerToken);
    }

    // ---- InvalidateCacheAsync ----

    [Fact]
    public async Task InvalidateCacheAsync_ClearsCache_SoNextCallHitsDb()
    {
        await using var db = CreateDb(nameof(InvalidateCacheAsync_ClearsCache_SoNextCallHitsDb));
        db.AuthSettings.Add(new AuthSettings { Id = 1, Mode = "Bearer", BearerToken = "initial" });
        await db.SaveChangesAsync();

        using var cache = CreateCache();
        var svc = new AuthSettingsService(db, cache);

        // Populate cache.
        await svc.GetSettingsAsync();

        // Mutate DB and invalidate cache.
        var row = await db.AuthSettings.FirstAsync();
        row.BearerToken = "updated";
        await db.SaveChangesAsync();
        await svc.InvalidateCacheAsync();

        // Next call must hit DB and return the updated value.
        var result = await svc.GetSettingsAsync();

        Assert.Equal("updated", result.BearerToken);
    }

    // ---- InvalidateCacheAsync: no-op when cache is empty ----

    [Fact]
    public async Task InvalidateCacheAsync_DoesNotThrow_WhenCacheIsEmpty()
    {
        await using var db = CreateDb(nameof(InvalidateCacheAsync_DoesNotThrow_WhenCacheIsEmpty));
        using var cache = CreateCache();
        var svc = new AuthSettingsService(db, cache);

        // Should not throw even when nothing is cached.
        await svc.InvalidateCacheAsync();
    }
}
