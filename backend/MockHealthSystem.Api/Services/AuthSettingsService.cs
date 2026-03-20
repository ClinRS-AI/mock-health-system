using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MockHealthSystem.Infrastructure.Data;
using MockHealthSystem.Infrastructure.Data.Entities;

namespace MockHealthSystem.Api.Services;

public interface IAuthSettingsService
{
    Task<AuthSettings> GetSettingsAsync(CancellationToken cancellationToken = default);
    Task InvalidateCacheAsync();
}

public sealed class AuthSettingsService : IAuthSettingsService
{
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;
    private const string CacheKey = "AuthSettings:Singleton";

    public AuthSettingsService(AppDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<AuthSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue<AuthSettings>(CacheKey, out var cached) && cached is not null)
        {
            return cached;
        }

        var settings = await _db.AuthSettings
                           .AsNoTracking()
                           .FirstOrDefaultAsync(cancellationToken)
                       ?? new AuthSettings
                       {
                           Id = 1,
                           Mode = "None",
                       };

        _cache.Set(CacheKey, settings, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
        });

        return settings;
    }

    public Task InvalidateCacheAsync()
    {
        _cache.Remove(CacheKey);
        return Task.CompletedTask;
    }
}

