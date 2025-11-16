namespace Agents.Infrastructure.Persistence.Redis.Models;

/// <summary>
/// Data Transfer Object for caching BIM classification suggestions.
/// Separates domain entities from serialization concerns.
/// </summary>
public sealed class BimClassificationCacheDto
{
    public long BimElementId { get; init; }
    public string? SuggestedCommodityCode { get; init; }
    public string? SuggestedPricingCode { get; init; }
    public List<DerivedItemCacheDto> DerivedItems { get; init; } = new();
    public string ReasoningSummary { get; init; } = default!;
}

public sealed class DerivedItemCacheDto
{
    public string DerivedCommodityCode { get; init; } = default!;
    public string? DerivedPricingCode { get; init; }
    public string QuantityFormula { get; init; } = default!;
    public string QuantityUnit { get; init; } = default!;
}
