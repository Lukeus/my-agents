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

        var redisValues = new RedisValue[]
        {
            System.Text.Json.JsonSerializer.Serialize(suggestion1),
            RedisValue.Null,
            System.Text.Json.JsonSerializer.Serialize(suggestion2)
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
        var suggestion1 = System.Text.Json.JsonSerializer.Serialize(new BimClassificationSuggestion(
            bimElementId: 1,
            commodityCode: "CC-001",
            pricingCode: "PC-001",
            derivedItems: Array.Empty<DerivedItemSuggestion>(),
            reasoningSummary: "Test"
        ));

        _mockCache
            .Setup(c => c.GetAsync("bim:classification:hash1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes(suggestion1));
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
        _mockDatabase
            .Setup(db => db.HashIncrementAsync(
                "bim:classification:stats",
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
                "bim:classification:stats",
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
            .Setup(db => db.HashGetAllAsync("bim:classification:stats", It.IsAny<CommandFlags>()))
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
            db => db.HashGetAllAsync("bim:classification:stats", It.IsAny<CommandFlags>()),
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

        var suggestion1 = System.Text.Json.JsonSerializer.Serialize(new BimClassificationSuggestion(
            bimElementId: 1,
            commodityCode: "CC-001",
            pricingCode: "PC-001",
            derivedItems: Array.Empty<DerivedItemSuggestion>(),
            reasoningSummary: "Test"
        ));

        _mockCache
            .Setup(c => c.GetAsync("bim:classification:hash1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes(suggestion1));

        // Act
        var result = await _repository.GetManyByPatternHashesAsync(hashes);

        // Assert
        result.Should().HaveCount(1);
        
        // Verify fallback to IDistributedCache
        _mockCache.Verify(
            c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
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

        // Assert
        _mockCache.Verify(
            c => c.SetAsync(
                "bim:classification:test-hash",
                It.IsAny<byte[]>(),
                It.Is<DistributedCacheEntryOptions>(o => o.AbsoluteExpirationRelativeToNow == TimeSpan.FromHours(1)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
