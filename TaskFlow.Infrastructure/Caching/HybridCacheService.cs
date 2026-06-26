using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces.Services;
using TaskFlow.Application.Options;

namespace TaskFlow.Infrastructure.Caching;

/// <summary>
/// Two-level cache: L1 = IMemoryCache (in-process, ~30 s), L2 = Redis (distributed, minutes).
/// L1 eliminates Redis round-trips for the hottest reads (members, columns) within a single replica.
/// On invalidation both layers are cleared so writes are always consistent.
/// </summary>
public class HybridCacheService(
    RedisCacheService redis,
    IMemoryCache memory,
    IOptions<CacheOptions> opts) : ICacheService
{
    private readonly TimeSpan _l1Expiry = TimeSpan.FromSeconds(opts.Value.L1ExpirySeconds);

    public async Task<T?> GetAsync<T>(string key, string? statsCategory = null, CancellationToken ct = default)
    {
        if (memory.TryGetValue(key, out T? cached))
            return cached;

        var value = await redis.GetAsync<T>(key, statsCategory, ct);
        if (value is not null)
            memory.Set(key, value, _l1Expiry);

        return value;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        memory.Set(key, value, _l1Expiry);
        await redis.SetAsync(key, value, expiry, ct);
    }

    public async Task InvalidateAsync(string key, CancellationToken ct = default)
    {
        memory.Remove(key);
        await redis.InvalidateAsync(key, ct);
    }

    public async Task InvalidateManyAsync(IEnumerable<string> keys, CancellationToken ct = default)
    {
        var list = keys.ToList();
        foreach (var k in list) memory.Remove(k);
        await redis.InvalidateManyAsync(list, ct);
    }

    public Task<CacheStatsDto> GetStatsAsync(CancellationToken ct = default)
        => redis.GetStatsAsync(ct);
}
