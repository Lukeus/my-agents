using Agents.Domain.BimClassification.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Agents.Tests.Unit.BimClassification;

[Trait("Category", "Unit")]
public class BimElementPromptModelTests
{
    [Fact]
    public void ToPromptString_WithAllProperties_GeneratesCompletePrompt()
    {
        // Arrange
        var model = new BimElementPromptModel
        {
            Id = 123,
            Category = "Ducts",
            Family = "Rectangular Duct",
            Type = "Supply",
            Spec = "HVAC-001",
            LocationType = "Outdoor",
            Material = "Galvanized Steel",
            LengthMm = 5000,
            WidthMm = 300,
            HeightMm = 200,
            DiameterMm = null,
            Meta = new Dictionary<string, string>
            {
                ["Manufacturer"] = "ACME Corp",
                ["FireRating"] = "Class A"
            }
        };

        // Act
        var result = model.ToPromptString();

        // Assert
        result.Should().Contain("ElementId: 123");
        result.Should().Contain("Category: Ducts");
        result.Should().Contain("Spec: HVAC-001");
        result.Should().Contain("LocationType: Outdoor");
        result.Should().Contain("Material: Galvanized Steel");
        result.Should().Contain("Family: Rectangular Duct");
        result.Should().Contain("Type: Supply");
        result.Should().Contain("Dimensions (mm):");
        result.Should().Contain("Length: 5000");
        result.Should().Contain("Width: 300");
        result.Should().Contain("Height: 200");
        result.Should().Contain("Metadata:");
        result.Should().Contain("Manufacturer: ACME Corp");
        result.Should().Contain("FireRating: Class A");
    }

    [Fact]
    public void ToPromptString_WithMinimalProperties_GeneratesBasicPrompt()
    {
        // Arrange
        var model = new BimElementPromptModel
        {
            Id = 456,
            Category = "Pipes"
        };

        // Act
        var result = model.ToPromptString();

        // Assert
        result.Should().Contain("ElementId: 456");
        result.Should().Contain("Category: Pipes");
        result.Should().NotContain("Spec:");
        result.Should().NotContain("LocationType:");
        result.Should().NotContain("Dimensions");
        result.Should().NotContain("Metadata:");
    }

    [Fact]
    public void ToPromptString_WithLongMetadata_TruncatesValues()
    {
        // Arrange
        var longValue = new string('x', 100);
        var model = new BimElementPromptModel
        {
            Id = 789,
            Category = "Test",
            Meta = new Dictionary<string, string>
            {
                ["LongField"] = longValue
            }
        };

        // Act
        var result = model.ToPromptString();

        // Assert
        result.Should().Contain("LongField:");
        result.Should().Contain("...");
        result.Should().NotContain(longValue); // Full value should be truncated
        // Check that it's truncated to 80 chars + "..."
        var metadataLine = result.Split('\n').First(l => l.Contains("LongField:"));
        metadataLine.Length.Should().BeLessThan(100);
    }

    [Fact]
    public void ToPromptString_WithOnlyDiameter_ShowsDimensionsSection()
    {
        // Arrange
        var model = new BimElementPromptModel
        {
            Id = 999,
            Category = "Pipes",
            DiameterMm = 150
        };

        // Act
        var result = model.ToPromptString();

        // Assert
        result.Should().Contain("Dimensions (mm):");
        result.Should().Contain("Diameter: 150");
        result.Should().NotContain("Length:");
        result.Should().NotContain("Width:");
        result.Should().NotContain("Height:");
    }

    [Fact]
    public void ToPromptString_WithNoDimensions_ExcludesDimensionsSection()
    {
        // Arrange
        var model = new BimElementPromptModel
        {
            Id = 111,
            Category = "Furniture"
        };

        // Act
        var result = model.ToPromptString();

        // Assert
        result.Should().NotContain("Dimensions");
    }

    [Fact]
    public void ToPromptString_WithEmptyMetadata_ExcludesMetadataSection()
    {
        // Arrange
        var model = new BimElementPromptModel
        {
            Id = 222,
            Category = "Equipment",
            Meta = new Dictionary<string, string>()
        };

        // Act
        var result = model.ToPromptString();

        // Assert
        result.Should().NotContain("Metadata:");
    }
}
