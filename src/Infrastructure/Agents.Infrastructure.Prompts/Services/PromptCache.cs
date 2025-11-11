using Agents.Infrastructure.Prompts.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Agents.Infrastructure.Prompts.Services;

/// <summary>
/// In-memory cache for frequently accessed prompts with invalidation support.
/// </summary>
public class PromptCache
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<PromptCache> _logger;
    private readonly MemoryCacheEntryOptions _defaultOptions;

    public PromptCache(IMemoryCache cache, ILogger<PromptCache> logger)
    {
        _cache = cache;
        _logger = logger;

        _defaultOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(30),
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2),
            Priority = CacheItemPriority.Normal
        };
    }

    /// <summary>
    /// Gets a prompt from cache or loads it using the provided factory.
    /// </summary>
    public async Task<Prompt> GetOrAddAsync(
        string key,
        Func<Task<Prompt>> factory,
        CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue<Prompt>(key, out var cachedPrompt) && cachedPrompt != null)
        {
            _logger.LogDebug("Cache hit for prompt: {Key}", key);
            return cachedPrompt;
        }

        _logger.LogDebug("Cache miss for prompt: {Key}", key);

        var prompt = await factory();
        
        _cache.Set(key, prompt, _defaultOptions);

        _logger.LogInformation("Cached prompt '{Name}' v{Version} with key: {Key}", 
            prompt.Metadata.Name, 
            prompt.Metadata.Version,
            key);

        return prompt;
    }

    /// <summary>
    /// Gets a prompt from cache by name and optionally version.
    /// </summary>
    public Prompt? Get(string name, string? version = null)
    {
        var key = GetCacheKey(name, version);
        
        if (_cache.TryGetValue<Prompt>(key, out var prompt))
        {
            _logger.LogDebug("Retrieved prompt from cache: {Key}", key);
            return prompt;
        }

        _logger.LogDebug("Prompt not found in cache: {Key}", key);
        return null;
    }

    /// <summary>
    /// Adds or updates a prompt in the cache.
    /// </summary>
    public void Set(Prompt prompt, TimeSpan? expiration = null)
    {
        var key = GetCacheKey(prompt.Metadata.Name, prompt.Metadata.Version);
        
        var options = expiration.HasValue
            ? new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiration.Value }
            : _defaultOptions;

        _cache.Set(key, prompt, options);

        _logger.LogInformation("Set prompt in cache: {Key}", key);
    }

    /// <summary>
    /// Invalidates a specific prompt from the cache.
    /// </summary>
    public void Invalidate(string name, string? version = null)
    {
        var key = GetCacheKey(name, version);
        _cache.Remove(key);
        
        _logger.LogInformation("Invalidated prompt from cache: {Key}", key);
    }

    /// <summary>
    /// Invalidates all cached prompts (clears the entire cache).
    /// </summary>
    public void InvalidateAll()
    {
        if (_cache is MemoryCache memoryCache)
        {
            memoryCache.Compact(1.0); // Remove all items
            _logger.LogInformation("Invalidated all prompts from cache");
        }
        else
        {
            _logger.LogWarning("Cannot invalidate all - cache type does not support compact");
        }
    }

    /// <summary>
    /// Checks if a prompt exists in cache.
    /// </summary>
    public bool Contains(string name, string? version = null)
    {
        var key = GetCacheKey(name, version);
        return _cache.TryGetValue(key, out _);
    }

    /// <summary>
    /// Gets cache statistics (if supported by the cache implementation).
    /// </summary>
    public CacheStatistics GetStatistics()
    {
        // Note: MemoryCache doesn't expose count/size directly
        // This is a simplified implementation
        return new CacheStatistics
        {
            // Would need custom cache wrapper to track these properly
            TotalItems = 0,
            HitCount = 0,
            MissCount = 0
        };
    }

    /// <summary>
    /// Generates a cache key from prompt name and version.
    /// </summary>
    private static string GetCacheKey(string name, string? version = null)
    {
        return version != null 
            ? $"prompt:{name}:{version}" 
            : $"prompt:{name}:latest";
    }
}

/// <summary>
/// Cache statistics.
/// </summary>
public class CacheStatistics
{
    public int TotalItems { get; set; }
    public long HitCount { get; set; }
    public long MissCount { get; set; }
    public double HitRate => MissCount == 0 ? 1.0 : (double)HitCount / (HitCount + MissCount);
}
