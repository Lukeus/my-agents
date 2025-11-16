using Agents.Domain.BimClassification.Entities;
using Agents.Domain.BimClassification.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace Agents.Infrastructure.Persistence.Redis.Repositories;

/// <summary>
/// Redis-based implementation of classification cache repository.
/// Provides distributed caching for BIM classification suggestions.
/// </summary>
public class RedisClassificationCacheRepository : IClassificationCacheRepository
{
    private readonly IDistributedCache _cache;
    private readonly IConnectionMultiplexer? _redis;
    private readonly ILogger<RedisClassificationCacheRepository> _logger;
    private const string KeyPrefix = "bim:classification:";
    private const string StatsKey = "bim:classification:stats";

    public RedisClassificationCacheRepository(
        IDistributedCache cache,
        ILogger<RedisClassificationCacheRepository> logger,
        IConnectionMultiplexer? redis = null)
    {
        _cache = cache;
        _redis = redis;
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
        var hashes = patternHashes.ToList();
        var result = new Dictionary<string, BimClassificationSuggestion>();

        // Use native Redis MGET for efficient batch retrieval
        if (_redis != null)
        {
            try
            {
                var db = _redis.GetDatabase();
                var keys = hashes.Select(h => (RedisKey)GetKey(h)).ToArray();
                var values = await db.StringGetAsync(keys);

                for (int i = 0; i < hashes.Count; i++)
                {
                    if (values[i].HasValue)
                    {
                        var suggestion = JsonSerializer.Deserialize<BimClassificationSuggestion>(values[i]!);
                        if (suggestion != null)
                        {
                            result[hashes[i]] = suggestion;
                        }
                    }
                }

                _logger.LogDebug("Batch retrieved {Count} cached items via MGET", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to use Redis MGET, falling back to sequential retrieval");
            }
        }

        // Fallback: sequential retrieval with IDistributedCache
        foreach (var hash in hashes)
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
        // Use Redis HGETALL for atomic read if available
        if (_redis != null)
        {
            try
            {
                var db = _redis.GetDatabase();
                var entries = await db.HashGetAllAsync(StatsKey);

                if (entries.Length == 0)
                {
                    return new CacheStatistics { HitCount = 0, MissCount = 0, TotalItems = 0 };
                }

                long hitCount = 0, missCount = 0, totalItems = 0;
                foreach (var entry in entries)
                {
                    if (entry.Name == "HitCount" && entry.Value.HasValue)
                        hitCount = (long)entry.Value;
                    else if (entry.Name == "MissCount" && entry.Value.HasValue)
                        missCount = (long)entry.Value;
                    else if (entry.Name == "TotalItems" && entry.Value.HasValue)
                        totalItems = (long)entry.Value;
                }
                return new CacheStatistics
                {
                    HitCount = hitCount,
                    MissCount = missCount,
                    TotalItems = totalItems
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read stats via HGETALL, falling back to JSON");
            }
        }

        // Fallback: JSON-based storage
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
    /// Increments hit count using atomic Redis operation when available.
    /// </summary>
    private async Task IncrementHitCountAsync()
    {
        try
        {
            // Use atomic HINCRBY if Redis is available
            if (_redis != null)
            {
                var db = _redis.GetDatabase();
                await db.HashIncrementAsync(StatsKey, "HitCount", 1);
                return;
            }

            // Fallback: best-effort with race condition
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
    /// Increments miss count using atomic Redis operation when available.
    /// </summary>
    private async Task IncrementMissCountAsync()
    {
        try
        {
            // Use atomic HINCRBY if Redis is available
            if (_redis != null)
            {
                var db = _redis.GetDatabase();
                await db.HashIncrementAsync(StatsKey, "MissCount", 1);
                return;
            }

            // Fallback: best-effort with race condition
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
