using Agents.Domain.BimClassification.Entities;
using Agents.Domain.BimClassification.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Agents.Infrastructure.Persistence.Redis.Repositories;

/// <summary>
/// Redis-based implementation of classification cache repository.
/// Provides distributed caching for BIM classification suggestions.
/// </summary>
public class RedisClassificationCacheRepository : IClassificationCacheRepository
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisClassificationCacheRepository> _logger;
    private const string KeyPrefix = "bim:classification:";
    private const string StatsKey = "bim:classification:stats";

    public RedisClassificationCacheRepository(
        IDistributedCache cache,
        ILogger<RedisClassificationCacheRepository> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<BimClassificationSuggestion?> GetByPatternHashAsync(
        string patternHash,
        CancellationToken cancellationToken = default)
    {
        var key = GetKey(patternHash);
        var json = await _cache.GetStringAsync(key, cancellationToken);

        if (json == null)
        {
            // Note: Statistics tracking is best-effort due to IDistributedCache limitations
            // For accurate stats in production, use StackExchange.Redis directly with HINCRBY
            _ = IncrementMissCountAsync(); // Fire and forget
            _logger.LogDebug("Cache miss for pattern hash: {PatternHash}", patternHash);
            return null;
        }

        _ = IncrementHitCountAsync(); // Fire and forget
        _logger.LogDebug("Cache hit for pattern hash: {PatternHash}", patternHash);

        return JsonSerializer.Deserialize<BimClassificationSuggestion>(json);
    }

    public async Task SetByPatternHashAsync(
        string patternHash,
        BimClassificationSuggestion suggestion,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        var key = GetKey(patternHash);
        var json = JsonSerializer.Serialize(suggestion);

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromHours(24),
            SlidingExpiration = TimeSpan.FromHours(6)
        };

        await _cache.SetStringAsync(key, json, options, cancellationToken);
        _logger.LogInformation("Cached classification for pattern hash: {PatternHash}", patternHash);
    }

    public async Task<IDictionary<string, BimClassificationSuggestion>> GetManyByPatternHashesAsync(
        IEnumerable<string> patternHashes,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, BimClassificationSuggestion>();

        // Redis doesn't have a native batch get in IDistributedCache
        // For production, consider using StackExchange.Redis directly for MGET
        foreach (var hash in patternHashes)
        {
            var suggestion = await GetByPatternHashAsync(hash, cancellationToken);
            if (suggestion != null)
            {
                result[hash] = suggestion;
            }
        }

        return result;
    }

    public async Task InvalidateByPatternHashAsync(
        string patternHash,
        CancellationToken cancellationToken = default)
    {
        var key = GetKey(patternHash);
        await _cache.RemoveAsync(key, cancellationToken);
        _logger.LogInformation("Invalidated cache for pattern hash: {PatternHash}", patternHash);
    }

    public async Task<CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var statsJson = await _cache.GetStringAsync(StatsKey, cancellationToken);
        
        if (statsJson == null)
        {
            return new CacheStatistics { HitCount = 0, MissCount = 0, TotalItems = 0 };
        }

        return JsonSerializer.Deserialize<CacheStatistics>(statsJson) 
               ?? new CacheStatistics { HitCount = 0, MissCount = 0, TotalItems = 0 };
    }

    private static string GetKey(string patternHash)
    {
        return $"{KeyPrefix}{patternHash}";
    }

    /// <summary>
    /// Increments hit count. Note: Has race condition with IDistributedCache.
    /// For production: Use StackExchange.Redis directly with HINCRBY for atomic increments.
    /// </summary>
    private async Task IncrementHitCountAsync()
    {
        try
        {
            var stats = await GetStatisticsAsync();
            var updated = new CacheStatistics
            {
                HitCount = stats.HitCount + 1,
                MissCount = stats.MissCount,
                TotalItems = stats.TotalItems
            };
            await SetStatisticsAsync(updated);
        }
        catch (Exception ex)
        {
            // Statistics are best-effort, don't fail cache operations
            _logger.LogWarning(ex, "Failed to increment hit count");
        }
    }

    /// <summary>
    /// Increments miss count. Note: Has race condition with IDistributedCache.
    /// For production: Use StackExchange.Redis directly with HINCRBY for atomic increments.
    /// </summary>
    private async Task IncrementMissCountAsync()
    {
        try
        {
            var stats = await GetStatisticsAsync();
            var updated = new CacheStatistics
            {
                HitCount = stats.HitCount,
                MissCount = stats.MissCount + 1,
                TotalItems = stats.TotalItems
            };
            await SetStatisticsAsync(updated);
        }
        catch (Exception ex)
        {
            // Statistics are best-effort, don't fail cache operations
            _logger.LogWarning(ex, "Failed to increment miss count");
        }
    }

    private async Task SetStatisticsAsync(CacheStatistics stats)
    {
        var json = JsonSerializer.Serialize(stats);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7)
        };
        await _cache.SetStringAsync(StatsKey, json, options);
    }
}
