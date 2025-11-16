using Agents.Domain.BimClassification.Entities;
using Agents.Domain.BimClassification.Interfaces;
using Agents.Infrastructure.Persistence.Redis.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;
using System.Text.Json.Serialization;

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
    private readonly string _instanceName;
    private const string KeyPrefix = "bim:classification:";
    private const string StatsKey = "bim:classification:stats";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    // Mapping methods between domain entity and cache DTO
    private static BimClassificationCacheDto ToDto(BimClassificationSuggestion entity) => new()
    {
        BimElementId = entity.BimElementId,
        SuggestedCommodityCode = entity.SuggestedCommodityCode,
        SuggestedPricingCode = entity.SuggestedPricingCode,
        ReasoningSummary = entity.ReasoningSummary,
        DerivedItems = entity.DerivedItems.Select(d => new DerivedItemCacheDto
        {
            DerivedCommodityCode = d.DerivedCommodityCode,
            DerivedPricingCode = d.DerivedPricingCode,
            QuantityFormula = d.QuantityFormula,
            QuantityUnit = d.QuantityUnit
        }).ToList()
    };

    private static BimClassificationSuggestion FromDto(BimClassificationCacheDto dto) => new(
        bimElementId: dto.BimElementId,
        commodityCode: dto.SuggestedCommodityCode,
        pricingCode: dto.SuggestedPricingCode,
        derivedItems: dto.DerivedItems.Select(d => new DerivedItemSuggestion
        {
            DerivedCommodityCode = d.DerivedCommodityCode,
            DerivedPricingCode = d.DerivedPricingCode,
            QuantityFormula = d.QuantityFormula,
            QuantityUnit = d.QuantityUnit
        }),
        reasoningSummary: dto.ReasoningSummary
    );

    public RedisClassificationCacheRepository(
        IDistributedCache cache,
        ILogger<RedisClassificationCacheRepository> logger,
        IConnectionMultiplexer? redis = null,
        string instanceName = "BimClassification:")
    {
        _cache = cache;
        _redis = redis;
        _logger = logger;
        _instanceName = instanceName;
    }

    public async Task<BimClassificationSuggestion?> GetByPatternHashAsync(
        string patternHash,
        CancellationToken cancellationToken = default)
    {
        var key = GetKey(patternHash);
        string? json = null;

        // If Redis connection is available, read directly as string for MGET compatibility
        if (_redis != null)
        {
            var db = _redis.GetDatabase();
            var fullKey = GetFullKey(patternHash);
            var value = await db.StringGetAsync(fullKey);
            if (value.HasValue)
            {
                json = value.ToString();
            }
        }
        else
        {
            // Fallback: use IDistributedCache
            json = await _cache.GetStringAsync(key, cancellationToken);
        }

        if (json == null)
        {
            _ = IncrementMissCountAsync(); // Fire and forget
            _logger.LogDebug("Cache miss for pattern hash: {PatternHash}", patternHash);
            return null;
        }

        _ = IncrementHitCountAsync(); // Fire and forget
        _logger.LogDebug("Cache hit for pattern hash: {PatternHash}", patternHash);

        var dto = JsonSerializer.Deserialize<BimClassificationCacheDto>(json, JsonOptions);
        return dto != null ? FromDto(dto) : null;
    }

    public async Task SetByPatternHashAsync(
        string patternHash,
        BimClassificationSuggestion suggestion,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        var key = GetKey(patternHash);
        var dto = ToDto(suggestion);
        var json = JsonSerializer.Serialize(dto, JsonOptions);

        // If Redis connection is available, store directly as string for MGET compatibility
        if (_redis != null)
        {
            var db = _redis.GetDatabase();
            var fullKey = GetFullKey(patternHash);
            var expirationTime = expiration ?? TimeSpan.FromHours(24);
            await db.StringSetAsync(fullKey, json, expirationTime);
            _logger.LogInformation("Cached classification for pattern hash: {PatternHash}", patternHash);
            return;
        }

        // Fallback: use IDistributedCache (stores as Redis HASH)
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
                // Include instance name prefix for direct Redis operations
                var keys = hashes.Select(h => (RedisKey)GetFullKey(h)).ToArray();

                _logger.LogDebug("MGET retrieving {Count} keys. First 3: {Keys}",
                    keys.Length, string.Join(", ", keys.Take(3).Select(k => k.ToString())));

                var values = await db.StringGetAsync(keys);

                for (int i = 0; i < hashes.Count; i++)
                {
                    if (values[i].HasValue)
                    {
                        try
                        {
                            var dto = JsonSerializer.Deserialize<BimClassificationCacheDto>(values[i]!, JsonOptions);
                            if (dto != null)
                            {
                                result[hashes[i]] = FromDto(dto);
                            }
                            else
                            {
                                _logger.LogWarning("Deserialized null DTO for hash {Hash}", hashes[i]);
                            }
                        }
                        catch (JsonException jsonEx)
                        {
                            _logger.LogError(jsonEx, "Failed to deserialize cached item for hash {Hash}: {Json}",
                                hashes[i], values[i].ToString());
                        }
                    }
                    else
                    {
                        _logger.LogDebug("No value found for hash {Hash} at index {Index}", hashes[i], i);
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

        // If Redis connection is available, delete directly
        if (_redis != null)
        {
            var db = _redis.GetDatabase();
            var fullKey = GetFullKey(patternHash);
            await db.KeyDeleteAsync(fullKey);
            _logger.LogInformation("Invalidated cache for pattern hash: {PatternHash}", patternHash);
            return;
        }

        // Fallback: use IDistributedCache
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
                var entries = await db.HashGetAllAsync(_instanceName + StatsKey);

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

    /// <summary>
    /// Gets the cache key without instance prefix (used by IDistributedCache).
    /// IDistributedCache automatically prepends the instance name.
    /// </summary>
    private static string GetKey(string patternHash)
    {
        return $"{KeyPrefix}{patternHash}";
    }

    /// <summary>
    /// Gets the full Redis key including instance prefix (used for direct Redis operations).
    /// Direct Redis operations require manual instance name prefixing.
    /// </summary>
    private string GetFullKey(string patternHash)
    {
        return _instanceName + GetKey(patternHash);
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
                await db.HashIncrementAsync(_instanceName + StatsKey, "HitCount", 1);
                return;
            }

            // Fallback: Use retry logic with optimistic concurrency
            const int maxRetries = 3;
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                var stats = await GetStatisticsAsync();
                var updated = new CacheStatistics
                {
                    HitCount = stats.HitCount + 1,
                    MissCount = stats.MissCount,
                    TotalItems = stats.TotalItems
                };

                // Try to set with a version check using conditional set
                if (await TrySetStatisticsAsync(updated, stats))
                {
                    return;
                }

                // Wait before retry with exponential backoff
                if (attempt < maxRetries - 1)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(10 * Math.Pow(2, attempt)));
                }
            }

            _logger.LogWarning("Failed to increment hit count after {MaxRetries} attempts", maxRetries);
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
                await db.HashIncrementAsync(_instanceName + StatsKey, "MissCount", 1);
                return;
            }

            // Fallback: Use retry logic with optimistic concurrency
            const int maxRetries = 3;
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                var stats = await GetStatisticsAsync();
                var updated = new CacheStatistics
                {
                    HitCount = stats.HitCount,
                    MissCount = stats.MissCount + 1,
                    TotalItems = stats.TotalItems
                };

                // Try to set with a version check using conditional set
                if (await TrySetStatisticsAsync(updated, stats))
                {
                    return;
                }

                // Wait before retry with exponential backoff
                if (attempt < maxRetries - 1)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(10 * Math.Pow(2, attempt)));
                }
            }

            _logger.LogWarning("Failed to increment miss count after {MaxRetries} attempts", maxRetries);
        }
        catch (Exception ex)
        {
            // Statistics are best-effort, don't fail cache operations
            _logger.LogWarning(ex, "Failed to increment miss count");
        }
    }

    /// <summary>
    /// Attempts to set statistics with optimistic concurrency control.
    /// Returns true if successful, false if there was a concurrent modification.
    /// </summary>
    private async Task<bool> TrySetStatisticsAsync(CacheStatistics newStats, CacheStatistics expectedStats)
    {
        // Read current stats to check if they match expected
        var currentStats = await GetStatisticsAsync();

        // Check if stats have changed since we read them (optimistic concurrency check)
        if (currentStats.HitCount != expectedStats.HitCount ||
            currentStats.MissCount != expectedStats.MissCount ||
            currentStats.TotalItems != expectedStats.TotalItems)
        {
            return false; // Concurrent modification detected
        }

        // No concurrent modification, safe to update
        await SetStatisticsAsync(newStats);
        return true;
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
