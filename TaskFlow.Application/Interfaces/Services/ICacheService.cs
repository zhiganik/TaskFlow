using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Interfaces.Services;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, string? statsCategory = null, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default);
    Task InvalidateAsync(string key, CancellationToken ct = default);
    Task InvalidateManyAsync(IEnumerable<string> keys, CancellationToken ct = default);
    Task<CacheStatsDto> GetStatsAsync(CancellationToken ct = default);
}
