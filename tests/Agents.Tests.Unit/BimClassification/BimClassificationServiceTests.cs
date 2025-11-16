using Agents.Application.Core;
using Agents.Application.BimClassification;
using Agents.Application.BimClassification.Services;
using Agents.Domain.BimClassification.Entities;
using Agents.Domain.BimClassification.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Agents.Tests.Unit.BimClassification;

public class BimClassificationServiceTests
{
    private readonly Mock<IBimElementRepository> _mockElementRepository;
    private readonly Mock<IClassificationCacheRepository> _mockCacheRepository;
    private readonly BimClassificationAgent _agent;
    private readonly Mock<ILogger<BimClassificationService>> _mockLogger;
    private readonly BimClassificationService _service;

    public BimClassificationServiceTests()
    {
        _mockElementRepository = new Mock<IBimElementRepository>();
        _mockCacheRepository = new Mock<IClassificationCacheRepository>();
        
        // Create a concrete agent instance (cannot mock BaseAgent.ExecuteAsync as it's not virtual)
        var mockLLMProvider = new Mock<Agents.Application.Core.ILLMProvider>();
        var mockPromptLoader = new Mock<Agents.Infrastructure.Prompts.Services.IPromptLoader>();
        var mockEventPublisher = new Mock<Agents.Domain.Core.Interfaces.IEventPublisher>();
        var mockAgentLogger = new Mock<ILogger<BimClassificationAgent>>();
        
        _agent = new BimClassificationAgent(
            mockLLMProvider.Object,
            mockPromptLoader.Object,
            mockEventPublisher.Object,
            mockAgentLogger.Object);
        
        _mockLogger = new Mock<ILogger<BimClassificationService>>();

        _service = new BimClassificationService(
            _mockElementRepository.Object,
            _mockCacheRepository.Object,
            _agent,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ClassifyBatchAsync_WithCachedPatterns_ShouldReturnCachedResults()
    {
        // Arrange
        var elementIds = new List<long> { 1, 2, 3, 4, 5 };
        
        var pattern = new BimPattern
        {
            PatternKey = "Ducts_Rectangular_Standard",
            Category = "Ducts",
            Family = "Rectangular",
            Type = "Standard",
            ElementCount = 5,
            SampleElements = new List<BimElementView>
            {
                new BimElementView { Id = 1, Category = "Ducts" }
            }
        };

        var cachedSuggestion = new BimClassificationSuggestion(
            bimElementId: 1,
            commodityCode: "DUCT-001",
            pricingCode: "PRICE-001",
            derivedItems: new List<DerivedItemSuggestion>(),
            reasoningSummary: "Cached suggestion");

        _mockElementRepository
            .Setup(x => x.GetPatternsByElementIdsAsync(elementIds, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BimPattern> { pattern });

        _mockCacheRepository
            .Setup(x => x.GetManyByPatternHashesAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, BimClassificationSuggestion>
            {
                [pattern.GetPatternHash()] = cachedSuggestion
            });

        var context = new AgentContext();

        // Act
        var result = await _service.ClassifyBatchAsync(elementIds, context);

        // Assert
        result.TotalElements.Should().Be(5);
        result.TotalPatterns.Should().Be(1);
        result.CachedPatterns.Should().Be(1);
        result.NewlyClassifiedPatterns.Should().Be(0);
        result.Suggestions.Should().ContainSingle();
        
        // Verify result came from cache (no new classifications)
        // Note: Cannot verify agent calls since ExecuteAsync is not virtual
    }

    [Fact]
    public async Task ClassifyBatchAsync_WithUncachedPatterns_ShouldAggregateCorrectly()
    {
        // Arrange - Test focuses on pattern aggregation, not LLM classification
        var elementIds = new List<long> { 1, 2, 3 };
        
        var pattern = new BimPattern
        {
            PatternKey = "Pipes_Round_Copper",
            Category = "Pipes",
            Family = "Round",
            Type = "Copper",
            ElementCount = 3,
            SampleElements = new List<BimElementView>
            {
                new BimElementView { Id = 1, Category = "Pipes" }
            }
        };

        _mockElementRepository
            .Setup(x => x.GetPatternsByElementIdsAsync(elementIds, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BimPattern> { pattern });

        _mockCacheRepository
            .Setup(x => x.GetManyByPatternHashesAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, BimClassificationSuggestion>()); // Empty cache

        var context = new AgentContext();

        // Act - Will attempt classification but fail gracefully without LLM setup
        // This test verifies pattern aggregation works
        var result = await _service.ClassifyBatchAsync(elementIds, context);
        
        // Assert - Verify aggregation happened correctly
        result.TotalElements.Should().Be(3);
        result.TotalPatterns.Should().Be(1);
        result.CachedPatterns.Should().Be(0);
        // NewlyClassifiedPatterns will be 0 since agent needs LLM setup
    }

    [Fact]
    public async Task ClassifyBatchAsync_WithMultiplePatterns_ShouldAggregateCorrectly()
    {
        // Arrange
        var elementIds = Enumerable.Range(1, 100).Select(i => (long)i).ToList();
        
        var pattern1 = new BimPattern
        {
            PatternKey = "Ducts_A",
            Category = "Ducts",
            ElementCount = 50
        };

        var pattern2 = new BimPattern
        {
            PatternKey = "Pipes_B",
            Category = "Pipes",
            ElementCount = 50
        };

        _mockElementRepository
            .Setup(x => x.GetPatternsByElementIdsAsync(elementIds, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BimPattern> { pattern1, pattern2 });

        _mockCacheRepository
            .Setup(x => x.GetManyByPatternHashesAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, BimClassificationSuggestion>());

        var context = new AgentContext();

        // Act
        var result = await _service.ClassifyBatchAsync(elementIds, context);

        // Assert
        result.TotalElements.Should().Be(100);
        result.TotalPatterns.Should().Be(2);
    }

    [Fact]
    public async Task ClassifyBatchAsync_WithEmptyElementIds_ShouldReturnEmptyResult()
    {
        // Arrange
        var elementIds = new List<long>();
        var context = new AgentContext();

        _mockElementRepository
            .Setup(x => x.GetPatternsByElementIdsAsync(elementIds, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BimPattern>());
        
        _mockCacheRepository
            .Setup(x => x.GetManyByPatternHashesAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, BimClassificationSuggestion>());

        // Act
        var result = await _service.ClassifyBatchAsync(elementIds, context);

        // Assert
        result.TotalElements.Should().Be(0);
        result.TotalPatterns.Should().Be(0);
        result.Suggestions.Should().BeEmpty();
    }
}
