namespace Agents.Application.BimClassification.Responses;

/// <summary>
/// Data transfer object for deserializing LLM JSON responses into classification suggestions.
/// Maps to BimClassificationSuggestion domain entity.
/// </summary>
public sealed class BimClassificationSuggestionDto
{
    public long BimElementId { get; set; }
    public string? CommodityCode { get; set; }
    public string? PricingCode { get; set; }
    public List<DerivedItemSuggestionDto> DerivedItems { get; set; } = new();
    public string ReasoningSummary { get; set; } = default!;
}

public sealed class DerivedItemSuggestionDto
{
    public string DerivedCommodityCode { get; set; } = default!;
    public string? DerivedPricingCode { get; set; }
    public string QuantityFormula { get; set; } = default!;
    public string QuantityUnit { get; set; } = default!;
}
