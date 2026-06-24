using System.Text.Json;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TaskFlow.Application.Interfaces;
using TaskFlow.Application.Options;

namespace TaskFlow.Infrastructure.Caching;

public class RedisCacheService(IConnectionMultiplexer redis, IOptions<CacheOptions> opts) : ICacheService
{
    private readonly IDatabase _db = redis.GetDatabase();
    private readonly TimeSpan _defaultExpiry =
        TimeSpan.FromMinutes(opts.Value.DefaultExpiryMinutes);

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var value = await _db.StringGetAsync(key);
        return value.IsNullOrEmpty ? default : JsonSerializer.Deserialize<T>((string)value!);
    }

    public async Task SetAsync<T>(string key, T value,
        TimeSpan? expiry = null, CancellationToken ct = default)
        => await _db.StringSetAsync(key, JsonSerializer.Serialize(value), expiry ?? _defaultExpiry);

    public async Task InvalidateAsync(string key, CancellationToken ct = default)
        => await _db.KeyDeleteAsync(key);
}
