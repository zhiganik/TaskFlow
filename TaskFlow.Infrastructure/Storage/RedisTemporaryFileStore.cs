using StackExchange.Redis;
using TaskFlow.Application.Interfaces.Services;

namespace TaskFlow.Infrastructure.Storage;

public class RedisTemporaryFileStore(IConnectionMultiplexer redis) : ITemporaryFileStore
{
    private readonly IDatabase _db = redis.GetDatabase();

    public async Task StoreAsync(
        string key, Stream content, TimeSpan expiry, CancellationToken ct = default)
    {
        using var ms = new MemoryStream();
        await content.CopyToAsync(ms, ct);
        await _db.StringSetAsync(key, ms.ToArray(), expiry);
    }

    public async Task<byte[]?> GetAsync(string key, CancellationToken ct = default)
    {
        var value = await _db.StringGetAsync(key);
        return value.HasValue ? (byte[])value! : null;
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
        => await _db.KeyDeleteAsync(key);
}
