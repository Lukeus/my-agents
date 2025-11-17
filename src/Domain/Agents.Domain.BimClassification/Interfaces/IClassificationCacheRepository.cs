using Agents.Domain.BimClassification.Entities;

namespace Agents.Domain.BimClassification.Interfaces;

/// <summary>
/// Repository interface for caching classification suggestions.
/// Follows clean architecture - Domain defines the contract, Infrastructure implements it.
/// </summary>
public interface IClassificationCacheRepository
{
    /// <summary>
    /// Gets a cached classification suggestion by pattern hash.
    /// </summary>
    Task<BimClassificationSuggestion?> GetByPatternHashAsync(
        string patternHash,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Caches a classification suggestion for a pattern.
    /// </summary>
    Task SetByPatternHashAsync(
        string patternHash,
        BimClassificationSuggestion suggestion,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets multiple cached suggestions by pattern hashes.
    /// </summary>
    Task<IDictionary<string, BimClassificationSuggestion>> GetManyByPatternHashesAsync(
        IEnumerable<string> patternHashes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates cached suggestion for a pattern.
    /// </summary>
    Task InvalidateByPatternHashAsync(
        string patternHash,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cache statistics.
    /// </summary>
    Task<CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Cache statistics for classification cache.
/// </summary>
public sealed class CacheStatistics
{
    /// <summary>
    /// Gets the number of cache hits.
    /// </summary>
    public long HitCount { get; init; }

    /// <summary>
    /// Gets the number of cache misses.
    /// </summary>
    public long MissCount { get; init; }

    /// <summary>
    /// Gets the cache hit rate (hits / total requests).
    /// </summary>
    public double HitRate => MissCount == 0 ? 1.0 : (double)HitCount / (HitCount + MissCount);

    /// <summary>
    /// Gets the total number of items in the cache.
    /// </summary>
    public long TotalItems { get; init; }
}
