using System.Text.Json;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TaskFlow.Application.Caching;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces.Services;
using TaskFlow.Application.Options;

namespace TaskFlow.Infrastructure.Caching;

public class RedisCacheService(IConnectionMultiplexer redis, IOptions<CacheOptions> opts) : ICacheService
{
    private readonly IDatabase _db           = redis.GetDatabase();
    private readonly TimeSpan  _defaultExpiry = TimeSpan.FromMinutes(opts.Value.DefaultExpiryMinutes);

    public async Task<T?> GetAsync<T>(string key, string? statsCategory = null, CancellationToken ct = default)
    {
        var value = await _db.StringGetAsync(key);
        var hit   = !value.IsNullOrEmpty;

        if (statsCategory is not null)
            _ = _db.StringIncrementAsync(hit
                ? CacheKeys.HitCounter(statsCategory)
                : CacheKeys.MissCounter(statsCategory));

        return hit ? JsonSerializer.Deserialize<T>((string)value!) : default;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
        => await _db.StringSetAsync(key, JsonSerializer.Serialize(value), expiry ?? _defaultExpiry);

    public async Task InvalidateAsync(string key, CancellationToken ct = default)
        => await _db.KeyDeleteAsync(key);

    public async Task InvalidateManyAsync(IEnumerable<string> keys, CancellationToken ct = default)
        => await _db.KeyDeleteAsync(keys.Select(k => (RedisKey)k).ToArray());

    public async Task<CacheStatsDto> GetStatsAsync(CancellationToken ct = default)
    {
        string[] categories =
        [
            CacheKeys.Category.UserWorkspaces,
            CacheKeys.Category.Members,
            CacheKeys.Category.Columns,
        ];

        var byCategory = new Dictionary<string, CategoryStats>(categories.Length);
        long totalHits = 0, totalMisses = 0;

        foreach (var cat in categories)
        {
            var hits   = (long?)await _db.StringGetAsync(CacheKeys.HitCounter(cat))  ?? 0;
            var misses = (long?)await _db.StringGetAsync(CacheKeys.MissCounter(cat)) ?? 0;
            var total  = hits + misses;
            byCategory[cat] = new CategoryStats(hits, misses, Pct(hits, total));
            totalHits   += hits;
            totalMisses += misses;
        }

        return new CacheStatsDto(byCategory, totalHits, totalMisses, Pct(totalHits, totalHits + totalMisses));
    }

    private static double Pct(long part, long total) =>
        total == 0 ? 0d : Math.Round(part * 100.0 / total, 1);
}
