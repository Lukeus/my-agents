using Agents.Domain.BimClassification.Entities;
using Agents.Infrastructure.Persistence.Redis.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace Agents.Tests.Integration.BimClassification;

public class RedisClassificationCacheIntegrationTests : IAsyncLifetime
{
    private RedisContainer? _redisContainer;
    private IConnectionMultiplexer? _redis;
    private IDistributedCache? _cache;
    private RedisClassificationCacheRepository? _repository;

    public async Task InitializeAsync()
    {
        // Start Redis container
        _redisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();

        await _redisContainer.StartAsync();

        // Create Redis connection
        _redis = await ConnectionMultiplexer.ConnectAsync(_redisContainer.GetConnectionString());

        // Create distributed cache
        var options = Options.Create(new RedisCacheOptions
        {
            Configuration = _redisContainer.GetConnectionString(),
            InstanceName = "BimClassification:"
        });
        _cache = new RedisCache(options);

        // Create repository
        _repository = new RedisClassificationCacheRepository(
            _cache,
            NullLogger<RedisClassificationCacheRepository>.Instance,
            _redis);
    }

    public async Task DisposeAsync()
    {
        if (_redis != null)
        {
            await _redis.CloseAsync();
            _redis.Dispose();
        }

        if (_redisContainer != null)
        {
            await _redisContainer.DisposeAsync();
        }
    }

    [Fact]
    public async Task GetManyByPatternHashesAsync_RealRedis_UsesMGETEfficiently()
    {
        // Arrange: Store 100 suggestions in Redis
        var suggestions = Enumerable.Range(1, 100)
            .Select(i => (
                Hash: $"pattern-{i:000}",
                Suggestion: new BimClassificationSuggestion(
                    bimElementId: i,
                    commodityCode: $"CC-{i:000}",
                    pricingCode: $"PC-{i:000}",
                    derivedItems: Array.Empty<DerivedItemSuggestion>(),
                    reasoningSummary: $"Element Type {i} classification"
                )
            ))
            .ToList();

        // Store all suggestions
        foreach (var (hash, suggestion) in suggestions)
        {
            await _repository!.SetByPatternHashAsync(hash, suggestion, TimeSpan.FromMinutes(10));
        }

        // Act: Retrieve all 100 in batch
        var hashes = suggestions.Select(s => s.Hash).ToArray();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var results = await _repository.GetManyByPatternHashesAsync(hashes);
        stopwatch.Stop();

        // Assert
        results.Should().HaveCount(100);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100, 
            "Batch MGET should be very fast (<100ms) for 100 items");

        // Verify all items retrieved correctly
        foreach (var (hash, expectedSuggestion) in suggestions)
        {
            results.Should().ContainKey(hash);
            results[hash].SuggestedCommodityCode.Should().Be(expectedSuggestion.SuggestedCommodityCode);
            results[hash].SuggestedPricingCode.Should().Be(expectedSuggestion.SuggestedPricingCode);
        }
    }

    [Fact]
    public async Task GetManyByPatternHashesAsync_MixedCacheHits_RetrievesCorrectly()
    {
        // Arrange: Store only even numbered patterns (50% hit rate)
        var allHashes = Enumerable.Range(1, 100).Select(i => $"pattern-{i:000}").ToList();
        var storedHashes = allHashes.Where((_, i) => i % 2 == 0).ToList();

        foreach (var hash in storedHashes)
        {
            await _repository!.SetByPatternHashAsync(hash, new BimClassificationSuggestion(
                bimElementId: 1,
                commodityCode: hash,
                pricingCode: "PC-TEST",
                derivedItems: Array.Empty<DerivedItemSuggestion>(),
                reasoningSummary: "Test"
            ));
        }

        // Act: Try to retrieve all 100
        var results = await _repository!.GetManyByPatternHashesAsync(allHashes);

        // Assert: Should get 50 results (every other one)
        results.Should().HaveCount(50);
        foreach (var hash in storedHashes)
        {
            results.Should().ContainKey(hash);
        }
    }

    [Fact]
    public async Task AtomicIncrements_ConcurrentOperations_ProduceAccurateCounts()
    {
        // Arrange: Simulate concurrent cache operations
        var tasks = new List<Task>();
        var hitCount = 100;
        var missCount = 50;

        // Store a single pattern for hit testing
        await _repository!.SetByPatternHashAsync("test-pattern", new BimClassificationSuggestion(
            bimElementId: 1,
            commodityCode: "TEST",
            pricingCode: "PC-TEST",
            derivedItems: Array.Empty<DerivedItemSuggestion>(),
            reasoningSummary: "Test"
        ));

        // Act: Simulate concurrent hits and misses
        for (int i = 0; i < hitCount; i++)
        {
            tasks.Add(Task.Run(async () => 
                await _repository.GetByPatternHashAsync("test-pattern")));
        }

        for (int i = 0; i < missCount; i++)
        {
            tasks.Add(Task.Run(async () => 
                await _repository.GetByPatternHashAsync($"nonexistent-{i}")));
        }

        await Task.WhenAll(tasks);

        // Allow time for fire-and-forget increments to complete
        await Task.Delay(500);

        // Assert: Stats should be accurate despite concurrency
        var stats = await _repository.GetStatisticsAsync();
        stats.HitCount.Should().Be(hitCount, 
            "Atomic HINCRBY should handle concurrent increments correctly");
        stats.MissCount.Should().Be(missCount,
            "Atomic HINCRBY should handle concurrent increments correctly");
        stats.HitRate.Should().BeApproximately(0.667, 0.01);
    }

    [Fact]
    public async Task GetStatisticsAsync_RealRedis_UsesHashStructure()
    {
        // Arrange: Perform some operations
        await _repository!.SetByPatternHashAsync("hash1", new BimClassificationSuggestion(
            bimElementId: 1,
            commodityCode: "TEST",
            pricingCode: "PC-TEST",
            derivedItems: Array.Empty<DerivedItemSuggestion>(),
            reasoningSummary: "Test"
        ));

        await _repository.GetByPatternHashAsync("hash1"); // Hit
        await _repository.GetByPatternHashAsync("hash1"); // Hit
        await _repository.GetByPatternHashAsync("nonexistent"); // Miss

        // Allow increments to complete
        await Task.Delay(200);

        // Act
        var stats = await _repository.GetStatisticsAsync();

        // Assert
        stats.HitCount.Should().BeGreaterOrEqualTo(2);
        stats.MissCount.Should().BeGreaterOrEqualTo(1);

        // Verify Redis is using hash structure
        var db = _redis!.GetDatabase();
        var entries = await db.HashGetAllAsync("BimClassification:bim:classification:stats");
        entries.Should().NotBeEmpty("Stats should be stored as Redis hash");
    }

    [Fact]
    public async Task BatchOperations_PerformanceComparison_MGETVsSequential()
    {
        // Arrange: Store 50 patterns
        var hashes = Enumerable.Range(1, 50).Select(i => $"perf-test-{i:000}").ToArray();
        foreach (var hash in hashes)
        {
            await _repository!.SetByPatternHashAsync(hash, new BimClassificationSuggestion(
                bimElementId: 1,
                commodityCode: hash,
                pricingCode: "PC-PERF",
                derivedItems: Array.Empty<DerivedItemSuggestion>(),
                reasoningSummary: "Performance Test"
            ));
        }

        // Act & Measure: Batch retrieval with MGET
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var batchResults = await _repository!.GetManyByPatternHashesAsync(hashes);
        var batchTime = stopwatch.ElapsedMilliseconds;

        // Act & Measure: Sequential retrieval (fallback simulation)
        stopwatch.Restart();
        var sequentialResults = new Dictionary<string, BimClassificationSuggestion>();
        foreach (var hash in hashes)
        {
            var result = await _repository.GetByPatternHashAsync(hash);
            if (result != null)
            {
                sequentialResults[hash] = result;
            }
        }
        var sequentialTime = stopwatch.ElapsedMilliseconds;

        // Assert: Batch should be significantly faster
        batchResults.Should().HaveCount(50);
        sequentialResults.Should().HaveCount(50);
        
        batchTime.Should().BeLessThan(sequentialTime / 2, 
            "Batch MGET should be at least 2x faster than sequential GET operations");

        // Output performance metrics for visibility
        Console.WriteLine($"Batch MGET time: {batchTime}ms");
        Console.WriteLine($"Sequential GET time: {sequentialTime}ms");
        Console.WriteLine($"Performance improvement: {sequentialTime / (double)batchTime:F2}x");
    }

    [Fact]
    public async Task SetAndGet_ExpirationWorks_ItemsExpire()
    {
        // Arrange
        var hash = "expiring-pattern";
        var suggestion = new BimClassificationSuggestion(
            bimElementId: 1,
            commodityCode: "TEMP",
            pricingCode: "PC-TEMP",
            derivedItems: Array.Empty<DerivedItemSuggestion>(),
            reasoningSummary: "Temporary"
        );

        // Act: Store with 2 second expiration
        await _repository!.SetByPatternHashAsync(hash, suggestion, TimeSpan.FromSeconds(2));
        
        var immediate = await _repository.GetByPatternHashAsync(hash);
        await Task.Delay(2500); // Wait for expiration
        var afterExpiration = await _repository.GetByPatternHashAsync(hash);

        // Assert
        immediate.Should().NotBeNull("Item should exist immediately after set");
        afterExpiration.Should().BeNull("Item should be null after expiration");
    }

    [Fact]
    public async Task InvalidateByPatternHashAsync_RemovesItem()
    {
        // Arrange
        var hash = "to-invalidate";
        await _repository!.SetByPatternHashAsync(hash, new BimClassificationSuggestion(
            bimElementId: 1,
            commodityCode: "INV",
            pricingCode: "PC-INV",
            derivedItems: Array.Empty<DerivedItemSuggestion>(),
            reasoningSummary: "Invalidate Test"
        ));

        var beforeInvalidation = await _repository.GetByPatternHashAsync(hash);

        // Act
        await _repository.InvalidateByPatternHashAsync(hash);
        var afterInvalidation = await _repository.GetByPatternHashAsync(hash);

        // Assert
        beforeInvalidation.Should().NotBeNull();
        afterInvalidation.Should().BeNull();
    }
}
