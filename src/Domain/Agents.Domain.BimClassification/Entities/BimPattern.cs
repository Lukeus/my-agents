namespace Agents.Domain.BimClassification.Entities;

/// <summary>
/// Represents an aggregated pattern of similar BIM elements.
/// Used to reduce tokenization overhead by grouping similar elements.
/// </summary>
public sealed class BimPattern
{
    public string PatternKey { get; init; } = default!;
    public string Category { get; init; } = default!;
    public string? Family { get; init; }
    public string? Type { get; init; }
    public string? Material { get; init; }
    public string? LocationType { get; init; }
    public long ElementCount { get; init; }
    
    /// <summary>
    /// Representative sample of elements matching this pattern.
    /// Used for LLM analysis instead of all elements.
    /// </summary>
    public IReadOnlyList<BimElementView> SampleElements { get; init; } = Array.Empty<BimElementView>();
    
    /// <summary>
    /// Statistical summary of dimensions across all elements in pattern.
    /// </summary>
    public DimensionStatistics? DimensionStats { get; init; }
    
    /// <summary>
    /// Hash of pattern characteristics for cache keying.
    /// </summary>
    public string GetPatternHash()
    {
        var normalized = $"{Category}_{Family}_{Type}_{Material}_{LocationType}".ToLowerInvariant();
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(normalized)))[..16];
    }
}

/// <summary>
/// Statistical summary of dimensions for a pattern group.
/// </summary>
public sealed class DimensionStatistics
{
    public decimal? LengthMin { get; init; }
    public decimal? LengthMax { get; init; }
    public decimal? LengthAvg { get; init; }
    public decimal? WidthMin { get; init; }
    public decimal? WidthMax { get; init; }
    public decimal? WidthAvg { get; init; }
    public decimal? HeightMin { get; init; }
    public decimal? HeightMax { get; init; }
    public decimal? HeightAvg { get; init; }
    public decimal? DiameterMin { get; init; }
    public decimal? DiameterMax { get; init; }
    public decimal? DiameterAvg { get; init; }
}
