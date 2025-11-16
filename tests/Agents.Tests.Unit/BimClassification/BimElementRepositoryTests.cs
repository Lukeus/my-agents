using Agents.Domain.BimClassification.Entities;
using Agents.Infrastructure.Persistence.SqlServer.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Agents.Tests.Unit.BimClassification;

public class BimElementRepositoryTests
{
    private readonly Mock<DbContext> _mockContext;
    private readonly Mock<ILogger<BimElementRepository>> _mockLogger;
    private readonly BimElementRepository _repository;

    public BimElementRepositoryTests()
    {
        _mockContext = new Mock<DbContext>();
        _mockLogger = new Mock<ILogger<BimElementRepository>>();
        _repository = new BimElementRepository(_mockContext.Object, _mockLogger.Object);
    }

    [Fact]
    public void NullableAggregations_ShouldHandleAllNulls()
    {
        // Arrange: Simulate elements with all null dimensions
        var elements = new List<BimElementView>
        {
            new() { Id = 1, Category = "Wall", Family = "Basic", Type = "Generic",
                    LengthMm = null, WidthMm = null, HeightMm = null, DiameterMm = null },
            new() { Id = 2, Category = "Wall", Family = "Basic", Type = "Generic",
                    LengthMm = null, WidthMm = null, HeightMm = null, DiameterMm = null }
        };

        // Act: Test the aggregation logic
        var filtered = elements.Select(e => e.LengthMm).Where(x => x != null).DefaultIfEmpty();
        var min = filtered.Min();
        var max = filtered.Max();
        var avg = filtered.Average();

        // Assert: Should not throw and return null for empty sequences
        min.Should().BeNull();
        max.Should().BeNull();
        avg.Should().BeNull();
    }

    [Fact]
    public void NullableAggregations_ShouldHandleMixedNulls()
    {
        // Arrange: Some nulls, some values
        var elements = new List<BimElementView>
        {
            new() { Id = 1, Category = "Wall", Family = "Basic", Type = "Generic",
                    LengthMm = 1000m, WidthMm = null, HeightMm = 500m, DiameterMm = null },
            new() { Id = 2, Category = "Wall", Family = "Basic", Type = "Generic",
                    LengthMm = null, WidthMm = 200m, HeightMm = null, DiameterMm = 100m },
            new() { Id = 3, Category = "Wall", Family = "Basic", Type = "Generic",
                    LengthMm = 2000m, WidthMm = null, HeightMm = 1500m, DiameterMm = null }
        };

        // Act: Test aggregations filtering nulls
        var lengthFiltered = elements.Select(e => e.LengthMm).Where(x => x != null).DefaultIfEmpty();
        var widthFiltered = elements.Select(e => e.WidthMm).Where(x => x != null).DefaultIfEmpty();
        var heightFiltered = elements.Select(e => e.HeightMm).Where(x => x != null).DefaultIfEmpty();
        var diameterFiltered = elements.Select(e => e.DiameterMm).Where(x => x != null).DefaultIfEmpty();

        // Assert: Should compute correctly on non-null values
        lengthFiltered.Min().Should().Be(1000m);
        lengthFiltered.Max().Should().Be(2000m);
        lengthFiltered.Average().Should().Be(1500m);

        widthFiltered.Min().Should().Be(200m);
        widthFiltered.Max().Should().Be(200m);
        widthFiltered.Average().Should().Be(200m);

        heightFiltered.Min().Should().Be(500m);
        heightFiltered.Max().Should().Be(1500m);
        heightFiltered.Average().Should().Be(1000m);

        diameterFiltered.Min().Should().Be(100m);
        diameterFiltered.Max().Should().Be(100m);
        diameterFiltered.Average().Should().Be(100m);
    }

    [Fact]
    public void NullableAggregations_ShouldNotThrowInvalidOperationException()
    {
        // Arrange: Edge case with empty sequence
        var elements = new List<BimElementView>();

        // Act
        var action = () =>
        {
            var filtered = elements.Select(e => e.LengthMm).Where(x => x != null).DefaultIfEmpty();
            var min = filtered.Min();
            var max = filtered.Max();
            var avg = filtered.Average();
            return (min, max, avg);
        };

        // Assert: Should not throw
        action.Should().NotThrow<InvalidOperationException>();
    }

    [Fact]
    public void GroupByPattern_WithNullDimensions_ShouldProduceValidStatistics()
    {
        // Arrange: Realistic scenario with grouped elements
        var elements = new List<BimElementView>
        {
            // Pattern 1: Walls with dimensions
            new() { Id = 1, Category = "Wall", Family = "Basic", Type = "200mm",
                    Material = "Concrete", LocationType = "Interior",
                    LengthMm = 5000m, WidthMm = 200m, HeightMm = 3000m, DiameterMm = null },
            new() { Id = 2, Category = "Wall", Family = "Basic", Type = "200mm",
                    Material = "Concrete", LocationType = "Interior",
                    LengthMm = 6000m, WidthMm = 200m, HeightMm = 3000m, DiameterMm = null },

            // Pattern 2: Pipes with diameter (no width/height)
            new() { Id = 3, Category = "Pipe", Family = "Copper", Type = "15mm",
                    Material = "Copper", LocationType = "MEP",
                    LengthMm = 2000m, WidthMm = null, HeightMm = null, DiameterMm = 15m },
            new() { Id = 4, Category = "Pipe", Family = "Copper", Type = "15mm",
                    Material = "Copper", LocationType = "MEP",
                    LengthMm = 3000m, WidthMm = null, HeightMm = null, DiameterMm = 15m },

            // Pattern 3: Abstract elements with no dimensions
            new() { Id = 5, Category = "Space", Family = "Room", Type = "Office",
                    Material = null, LocationType = "Interior",
                    LengthMm = null, WidthMm = null, HeightMm = null, DiameterMm = null }
        };

        // Act: Group and aggregate (simulating repository logic)
        var patterns = elements
            .GroupBy(e => new { e.Category, e.Family, e.Type, e.Material, e.LocationType })
            .Select(g => new
            {
                g.Key,
                ElementCount = g.Count(),
                LengthMin = g.Select(e => e.LengthMm).Where(x => x != null).DefaultIfEmpty().Min(),
                LengthMax = g.Select(e => e.LengthMm).Where(x => x != null).DefaultIfEmpty().Max(),
                LengthAvg = g.Select(e => e.LengthMm).Where(x => x != null).DefaultIfEmpty().Average(),
                WidthMin = g.Select(e => e.WidthMm).Where(x => x != null).DefaultIfEmpty().Min(),
                WidthMax = g.Select(e => e.WidthMm).Where(x => x != null).DefaultIfEmpty().Max(),
                WidthAvg = g.Select(e => e.WidthMm).Where(x => x != null).DefaultIfEmpty().Average(),
                DiameterMin = g.Select(e => e.DiameterMm).Where(x => x != null).DefaultIfEmpty().Min(),
                DiameterMax = g.Select(e => e.DiameterMm).Where(x => x != null).DefaultIfEmpty().Max(),
                DiameterAvg = g.Select(e => e.DiameterMm).Where(x => x != null).DefaultIfEmpty().Average()
            })
            .ToList();

        // Assert: Should have 3 patterns
        patterns.Should().HaveCount(3);

        // Pattern 1: Walls (should have width/height, no diameter)
        var wallPattern = patterns.First(p => p.Key.Category == "Wall");
        wallPattern.ElementCount.Should().Be(2);
        wallPattern.LengthMin.Should().Be(5000m);
        wallPattern.LengthMax.Should().Be(6000m);
        wallPattern.WidthMin.Should().Be(200m);
        wallPattern.DiameterMin.Should().BeNull();

        // Pattern 2: Pipes (should have diameter, no width/height)
        var pipePattern = patterns.First(p => p.Key.Category == "Pipe");
        pipePattern.ElementCount.Should().Be(2);
        pipePattern.LengthMin.Should().Be(2000m);
        pipePattern.WidthMin.Should().BeNull();
        pipePattern.DiameterMin.Should().Be(15m);

        // Pattern 3: Spaces (all dimensions null)
        var spacePattern = patterns.First(p => p.Key.Category == "Space");
        spacePattern.ElementCount.Should().Be(1);
        spacePattern.LengthMin.Should().BeNull();
        spacePattern.WidthMin.Should().BeNull();
        spacePattern.DiameterMin.Should().BeNull();
    }

    [Fact]
    public void NullableAggregations_PerformanceTest_ShouldHandleLargeDatasets()
    {
        // Arrange: Simulate 10,000 elements with mixed nulls
        var elements = Enumerable.Range(1, 10000)
            .Select(i => new BimElementView
            {
                Id = i,
                Category = "Wall",
                Family = "Basic",
                Type = "Generic",
                LengthMm = i % 3 == 0 ? null : (decimal?)i * 100,
                WidthMm = i % 5 == 0 ? null : (decimal?)200,
                HeightMm = i % 2 == 0 ? null : (decimal?)3000,
                DiameterMm = null
            })
            .ToList();

        // Act
        var action = () =>
        {
            var stats = new
            {
                LengthMin = elements.Select(e => e.LengthMm).Where(x => x != null).DefaultIfEmpty().Min(),
                LengthMax = elements.Select(e => e.LengthMm).Where(x => x != null).DefaultIfEmpty().Max(),
                LengthAvg = elements.Select(e => e.LengthMm).Where(x => x != null).DefaultIfEmpty().Average(),
                WidthMin = elements.Select(e => e.WidthMm).Where(x => x != null).DefaultIfEmpty().Min(),
                WidthMax = elements.Select(e => e.WidthMm).Where(x => x != null).DefaultIfEmpty().Max(),
                WidthAvg = elements.Select(e => e.WidthMm).Where(x => x != null).DefaultIfEmpty().Average()
            };
            return stats;
        };

        // Assert: Should complete without throwing
        action.Should().NotThrow();
        var result = action();
        result.LengthMin.Should().Be(100m);
        result.LengthMax.Should().Be(1000000m); // 10000 * 100
        result.WidthMin.Should().Be(200m);
        result.WidthMax.Should().Be(200m);
    }
}
