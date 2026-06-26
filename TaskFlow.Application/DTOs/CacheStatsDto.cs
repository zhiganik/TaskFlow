namespace TaskFlow.Application.DTOs;

public record CategoryStats(long Hits, long Misses, double HitRatePct);

public record CacheStatsDto(
    IReadOnlyDictionary<string, CategoryStats> ByCategory,
    long   TotalHits,
    long   TotalMisses,
    double OverallHitRatePct);
