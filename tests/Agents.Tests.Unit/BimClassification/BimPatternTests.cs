using Agents.Domain.BimClassification.Entities;
using FluentAssertions;
using Xunit;

namespace Agents.Tests.Unit.BimClassification;

public class BimPatternTests
{
    [Fact]
    public void GetPatternHash_WithSameCharacteristics_ShouldReturnSameHash()
    {
        // Arrange
        var pattern1 = new BimPattern
        {
            Category = "Ducts",
            Family = "Rectangular",
            Type = "Standard",
            Material = "Galvanized",
            LocationType = "Indoor"
        };

        var pattern2 = new BimPattern
        {
            Category = "Ducts",
            Family = "Rectangular",
            Type = "Standard",
            Material = "Galvanized",
            LocationType = "Indoor"
        };

        // Act
        var hash1 = pattern1.GetPatternHash();
        var hash2 = pattern2.GetPatternHash();

        // Assert
        hash1.Should().Be(hash2);
        hash1.Should().HaveLength(16); // SHA256 truncated to 16 chars
    }

    [Fact]
    public void GetPatternHash_WithDifferentCharacteristics_ShouldReturnDifferentHash()
    {
        // Arrange
        var pattern1 = new BimPattern
        {
            Category = "Ducts",
            Family = "Rectangular",
            Type = "Standard",
            Material = "Galvanized",
            LocationType = "Indoor"
        };

        var pattern2 = new BimPattern
        {
            Category = "Ducts",
            Family = "Rectangular",
            Type = "Standard",
            Material = "Galvanized",
            LocationType = "Outdoor" // Different location
        };

        // Act
        var hash1 = pattern1.GetPatternHash();
        var hash2 = pattern2.GetPatternHash();

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void GetPatternHash_WithNullValues_ShouldHandleGracefully()
    {
        // Arrange
        var pattern = new BimPattern
        {
            Category = "Ducts",
            Family = null,
            Type = null,
            Material = null,
            LocationType = null
        };

        // Act
        var hash = pattern.GetPatternHash();

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().HaveLength(16);
    }

    [Fact]
    public void BimPattern_WithSampleElements_ShouldStoreCorrectly()
    {
        // Arrange
        var samples = new List<BimElementView>
        {
            new BimElementView { Id = 1, Category = "Ducts", LengthMm = 1000 },
            new BimElementView { Id = 2, Category = "Ducts", LengthMm = 2000 },
            new BimElementView { Id = 3, Category = "Ducts", LengthMm = 3000 }
        };

        // Act
        var pattern = new BimPattern
        {
            Category = "Ducts",
            ElementCount = 1000,
            SampleElements = samples
        };

        // Assert
        pattern.SampleElements.Should().HaveCount(3);
        pattern.SampleElements.Should().BeEquivalentTo(samples);
    }

    [Fact]
    public void DimensionStatistics_WithValidData_ShouldCalculateCorrectly()
    {
        // Arrange & Act
        var stats = new DimensionStatistics
        {
            LengthMin = 1000m,
            LengthMax = 5000m,
            LengthAvg = 3000m,
            WidthMin = 200m,
            WidthMax = 800m,
            WidthAvg = 500m
        };

        // Assert
        stats.LengthMin.Should().Be(1000m);
        stats.LengthMax.Should().Be(5000m);
        stats.LengthAvg.Should().Be(3000m);
        stats.WidthMin.Should().Be(200m);
        stats.WidthMax.Should().Be(800m);
        stats.WidthAvg.Should().Be(500m);
    }
}
