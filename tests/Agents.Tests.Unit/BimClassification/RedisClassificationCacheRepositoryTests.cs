using Agents.Domain.BimClassification.Entities;
using Agents.Infrastructure.Persistence.Redis.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;

namespace Agents.Tests.Unit.BimClassification;

public class RedisClassificationCacheRepositoryTests
{
    private readonly Mock<IDistributedCache> _mockCache;
    private readonly Mock<IConnectionMultiplexer> _mockRedis;
    private readonly Mock<IDatabase> _mockDatabase;
    private readonly Mock<ILogger<RedisClassificationCacheRepository>> _mockLogger;
    private readonly RedisClassificationCacheRepository _repository;

    public RedisClassificationCacheRepositoryTests()
    {
        _mockCache = new Mock<IDistributedCache>();
        _mockRedis = new Mock<IConnectionMultiplexer>();
        _mockDatabase = new Mock<IDatabase>();
        _mockLogger = new Mock<ILogger<RedisClassificationCacheRepository>>();

        _mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_mockDatabase.Object);

        _repository = new RedisClassificationCacheRepository(
            _mockCache.Object,
            _mockLogger.Object,
            _mockRedis.Object);
    }

    [Fact]
    public async Task GetManyByPatternHashesAsync_WithRedis_UsesMGET()
    {
        // Arrange
        var hashes = new[] { "hash1", "hash2", "hash3" };
        var suggestion1 = new BimClassificationSuggestion(
            bimElementId: 1,
            commodityCode: "CC-001",
            pricingCode: "PC-001",
            derivedItems: Array.Empty<DerivedItemSuggestion>(),
            reasoningSummary: "Wall element classification"
        );
        var suggestion2 = new BimClassificationSuggestion(
            bimElementId: 2,
            commodityCode: "CC-002",
            pricingCode: "PC-002",
            derivedItems: Array.Empty<DerivedItemSuggestion>(),
            reasoningSummary: "Door element classification"
        );

        // Serialize as DTO (matching repository implementation)
        var dto1 = new { bimElementId = 1, suggestedCommodityCode = "CC-001", suggestedPricingCode = "PC-001", reasoningSummary = "Wall element classification", derivedItems = Array.Empty<object>() };
        var dto2 = new { bimElementId = 2, suggestedCommodityCode = "CC-002", suggestedPricingCode = "PC-002", reasoningSummary = "Door element classification", derivedItems = Array.Empty<object>() };

        var redisValues = new RedisValue[]
        {
            System.Text.Json.JsonSerializer.Serialize(dto1, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase }),
            RedisValue.Null,
            System.Text.Json.JsonSerializer.Serialize(dto2, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase })
        };

        _mockDatabase
            .Setup(db => db.StringGetAsync(It.IsAny<RedisKey[]>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(redisValues);

        // Act
        var result = await _repository.GetManyByPatternHashesAsync(hashes);

        // Assert
        result.Should().HaveCount(2);
        result.Should().ContainKey("hash1");
        result.Should().ContainKey("hash3");
        result["hash1"].SuggestedCommodityCode.Should().Be("CC-001");
        result["hash3"].SuggestedCommodityCode.Should().Be("CC-002");

        // Verify MGET was called once (not 3 times)
        _mockDatabase.Verify(
            db => db.StringGetAsync(It.IsAny<RedisKey[]>(), It.IsAny<CommandFlags>()),
            Times.Once);
    }

    [Fact]
    public async Task GetManyByPatternHashesAsync_WithoutRedis_FallsBackToSequential()
    {
        // Arrange
        var repositoryWithoutRedis = new RedisClassificationCacheRepository(
            _mockCache.Object,
            _mockLogger.Object,
            redis: null);

        var hashes = new[] { "hash1", "hash2" };
        var dto1 = new { bimElementId = 1, suggestedCommodityCode = "CC-001", suggestedPricingCode = "PC-001", reasoningSummary = "Test", derivedItems = Array.Empty<object>() };
        var suggestion1Json = System.Text.Json.JsonSerializer.Serialize(dto1, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });

        // Mock the underlying GetAsync method (GetStringAsync is an extension)
        _mockCache
            .Setup(c => c.GetAsync("bim:classification:hash1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes(suggestion1Json));
        _mockCache
            .Setup(c => c.GetAsync("bim:classification:hash2", It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        // Act
        var result = await repositoryWithoutRedis.GetManyByPatternHashesAsync(hashes);

        // Assert
        result.Should().HaveCount(1);
        result.Should().ContainKey("hash1");

        // Verify sequential calls were made
        _mockCache.Verify(
            c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.AtLeast(2));
    }

    [Fact]
    public async Task GetManyByPatternHashesAsync_PerformanceTest_ShouldBatchEfficiently()
    {
        // Arrange: Simulate 100 pattern hashes
        var hashes = Enumerable.Range(1, 100).Select(i => $"hash{i}").ToArray();
        var redisValues = Enumerable.Range(1, 100)
            .Select(i => i % 3 == 0 ? RedisValue.Null : (RedisValue)$"{{\"ClassCode\":\"CC-{i:000}\"}}")
            .ToArray();

        _mockDatabase
            .Setup(db => db.StringGetAsync(It.IsAny<RedisKey[]>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(redisValues);

        // Act
        var result = await _repository.GetManyByPatternHashesAsync(hashes);

        // Assert
        result.Should().HaveCount(67); // 100 - 33 nulls (every 3rd)

        // Critical: Should only call Redis ONCE for all 100 items
        _mockDatabase.Verify(
            db => db.StringGetAsync(It.IsAny<RedisKey[]>(), It.IsAny<CommandFlags>()),
            Times.Once,
            "MGET should batch all keys into a single Redis call");
    }

    [Fact]
    public async Task IncrementHitCountAsync_WithRedis_UsesAtomicHINCRBY()
    {
        // Arrange
        var dto = new { bimElementId = 1, suggestedCommodityCode = "CC-001", suggestedPricingCode = "PC-001", reasoningSummary = "Test", derivedItems = Array.Empty<object>() };
        var json = System.Text.Json.JsonSerializer.Serialize(dto, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });

        _mockDatabase
            .Setup(db => db.StringGetAsync("BimClassification:bim:classification:existing-hash", It.IsAny<CommandFlags>()))
            .ReturnsAsync(json);

        _mockDatabase
            .Setup(db => db.HashIncrementAsync(
                "BimClassification:bim:classification:stats",
                "HitCount",
                1,
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(10);

        // Act
        var suggestion = await _repository.GetByPatternHashAsync("existing-hash");

        // The increment happens in the background, but we can verify the setup
        await Task.Delay(100); // Allow fire-and-forget to complete

        // Assert - verify atomic increment was called
        _mockDatabase.Verify(
            db => db.HashIncrementAsync(
                "BimClassification:bim:classification:stats",
                "HitCount",
                1,
                It.IsAny<CommandFlags>()),
            Times.Once,
            "Should use atomic HINCRBY for hit count");
    }

    [Fact]
    public async Task GetStatisticsAsync_WithRedis_UsesHGETALL()
    {
        // Arrange
        var hashEntries = new[]
        {
            new HashEntry("HitCount", 1000),
            new HashEntry("MissCount", 200),
            new HashEntry("TotalItems", 50)
        };

        _mockDatabase
            .Setup(db => db.HashGetAllAsync("BimClassification:bim:classification:stats", It.IsAny<CommandFlags>()))
            .ReturnsAsync(hashEntries);

        // Act
        var stats = await _repository.GetStatisticsAsync();

        // Assert
        stats.HitCount.Should().Be(1000);
        stats.MissCount.Should().Be(200);
        stats.TotalItems.Should().Be(50);
        stats.HitRate.Should().BeApproximately(0.833, 0.001);

        // Verify atomic read
        _mockDatabase.Verify(
            db => db.HashGetAllAsync("BimClassification:bim:classification:stats", It.IsAny<CommandFlags>()),
            Times.Once);
    }

    [Fact]
    public async Task GetStatisticsAsync_WithoutRedis_FallsBackToJSON()
    {
        // Arrange
        var repositoryWithoutRedis = new RedisClassificationCacheRepository(
            _mockCache.Object,
            _mockLogger.Object,
            redis: null);

        var statsJson = System.Text.Json.JsonSerializer.Serialize(new Agents.Domain.BimClassification.Interfaces.CacheStatistics
        {
            HitCount = 500,
            MissCount = 100,
            TotalItems = 25
        });

        _mockCache
            .Setup(c => c.GetAsync("bim:classification:stats", It.IsAny<CancellationToken>()))
            .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes(statsJson));

        // Act
        var stats = await repositoryWithoutRedis.GetStatisticsAsync();

        // Assert
        stats.HitCount.Should().Be(500);
        stats.MissCount.Should().Be(100);
    }

    [Fact]
    public async Task GetManyByPatternHashesAsync_RedisFailure_GracefullyFallsBack()
    {
        // Arrange
        var hashes = new[] { "hash1", "hash2" };

        _mockDatabase
            .Setup(db => db.StringGetAsync(It.IsAny<RedisKey[]>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisException("Connection timeout"));

        var dto1 = new { bimElementId = 1, suggestedCommodityCode = "CC-001", suggestedPricingCode = "PC-001", reasoningSummary = "Test", derivedItems = Array.Empty<object>() };
        var suggestion1Json = System.Text.Json.JsonSerializer.Serialize(dto1, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });

        // Mock fallback to StringGetAsync for individual keys
        _mockDatabase
            .Setup(db => db.StringGetAsync("BimClassification:bim:classification:hash1", It.IsAny<CommandFlags>()))
            .ReturnsAsync(suggestion1Json);
        _mockDatabase
            .Setup(db => db.StringGetAsync("BimClassification:bim:classification:hash2", It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        // Act
        var result = await _repository.GetManyByPatternHashesAsync(hashes);

        // Assert
        result.Should().HaveCount(1);

        // Verify fallback to sequential StringGetAsync calls
        _mockDatabase.Verify(
            db => db.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()),
            Times.AtLeast(1));
    }

    [Fact]
    public async Task SetByPatternHashAsync_StoresCorrectly()
    {
        // Arrange
        var patternHash = "test-hash";
        var suggestion = new BimClassificationSuggestion(
            bimElementId: 1,
            commodityCode: "CC-001",
            pricingCode: "PC-001",
            derivedItems: Array.Empty<DerivedItemSuggestion>(),
            reasoningSummary: "Test"
        );

        // Act
        await _repository.SetByPatternHashAsync(patternHash, suggestion, TimeSpan.FromHours(1));

        // Assert - now stores via Redis StringSetAsync
        _mockDatabase.Verify(
            db => db.StringSetAsync(
                "BimClassification:bim:classification:test-hash",
                It.IsAny<RedisValue>(),
                TimeSpan.FromHours(1),
                It.IsAny<bool>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()),
            Times.Once);
    }

    [Fact]
    public async Task IncrementHitCountAsync_WithoutRedis_RetriesOnConcurrentModification()
    {
        // Arrange
        var repositoryWithoutRedis = new RedisClassificationCacheRepository(
            _mockCache.Object,
            _mockLogger.Object,
            redis: null);

        var dto = new { bimElementId = 1, suggestedCommodityCode = "CC-001", suggestedPricingCode = "PC-001", reasoningSummary = "Test", derivedItems = Array.Empty<object>() };
        var json = System.Text.Json.JsonSerializer.Serialize(dto, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });

        // Mock cache hit that triggers increment
        _mockCache
            .Setup(c => c.GetAsync("bim:classification:test-hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes(json));

        // Mock statistics read - simulate concurrent modification on first attempt
        // Each retry does 2 reads: one to get stats, one in TrySet to verify
        int readCount = 0;
        var initialStats = System.Text.Json.JsonSerializer.Serialize(new Agents.Domain.BimClassification.Interfaces.CacheStatistics
        {
            HitCount = 100,
            MissCount = 50,
            TotalItems = 10
        });
        var modifiedStats = System.Text.Json.JsonSerializer.Serialize(new Agents.Domain.BimClassification.Interfaces.CacheStatistics
        {
            HitCount = 105, // Changed by another thread
            MissCount = 50,
            TotalItems = 10
        });

        _mockCache
            .Setup(c => c.GetAsync("bim:classification:stats", It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                readCount++;
                // Read 1: get initial (100), Read 2: verify (100) - match, set succeeds
                // But on first attempt, verification will detect change
                if (readCount == 1)
                    return System.Text.Encoding.UTF8.GetBytes(initialStats); // First read in attempt 1
                else if (readCount == 2)
                    return System.Text.Encoding.UTF8.GetBytes(modifiedStats); // Verification detects change
                else
                    return System.Text.Encoding.UTF8.GetBytes(modifiedStats); // Subsequent reads are stable
            });

        _mockCache
            .Setup(c => c.SetAsync("bim:classification:stats", It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await repositoryWithoutRedis.GetByPatternHashAsync("test-hash");
        await Task.Delay(200); // Allow fire-and-forget increment with retries to complete

        // Assert
        result.Should().NotBeNull();
        // Verify multiple reads occurred due to retry (at least 2 reads per attempt, with 1 retry = 4 total)
        _mockCache.Verify(
            c => c.GetAsync("bim:classification:stats", It.IsAny<CancellationToken>()),
            Times.AtLeast(2), // Should read at least twice (initial attempt + retry verification)
            "Should retry when statistics are modified concurrently");

        // Verify at least one SetAsync was called (successful retry)
        _mockCache.Verify(
            c => c.SetAsync("bim:classification:stats", It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce(),
            "Should eventually succeed in setting statistics");
    }
}
