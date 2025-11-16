using Agents.Domain.Core.Entities;
using Agents.Domain.BimClassification.Events;

namespace Agents.Domain.BimClassification.Entities;

/// <summary>
/// Aggregate root representing an agent-generated classification suggestion.
/// This is ADVISORY ONLY - never the canonical classification.
/// </summary>
public sealed class BimClassificationSuggestion : AggregateRoot<long>
{
    public long BimElementId { get; private set; }
    public string? SuggestedCommodityCode { get; private set; }
    public string? SuggestedPricingCode { get; private set; }
    public List<DerivedItemSuggestion> DerivedItems { get; private set; } = new();
    public string ReasoningSummary { get; private set; } = default!;
    public SuggestionStatus Status { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public DateTime? ReviewedUtc { get; private set; }
    public string? ReviewedBy { get; private set; }

    private BimClassificationSuggestion() : base(0) { }  // EF Core

    public BimClassificationSuggestion(
        long bimElementId,
        string? commodityCode,
        string? pricingCode,
        IEnumerable<DerivedItemSuggestion> derivedItems,
        string reasoningSummary)
        : base(0)  // ID assigned by DB
    {
        BimElementId = bimElementId;
        SuggestedCommodityCode = commodityCode;
        SuggestedPricingCode = pricingCode;
        DerivedItems = derivedItems.ToList();
        ReasoningSummary = reasoningSummary;
        Status = SuggestionStatus.Pending;
        CreatedUtc = DateTime.UtcNow;

        AddDomainEvent(new ClassificationSuggestedEvent
        {
            BimElementId = bimElementId,
            SuggestionId = Id,
            CommodityCode = commodityCode
        });
    }

    public void Approve(string approvedBy)
    {
        Status = SuggestionStatus.Approved;
        ReviewedUtc = DateTime.UtcNow;
        ReviewedBy = approvedBy;

        AddDomainEvent(new ClassificationSuggestionApprovedEvent
        {
            SuggestionId = Id,
            BimElementId = BimElementId,
            ApprovedBy = approvedBy
        });
    }

    public void Reject(string rejectedBy, string reason)
    {
        Status = SuggestionStatus.Rejected;
        ReviewedUtc = DateTime.UtcNow;
        ReviewedBy = rejectedBy;

        AddDomainEvent(new ClassificationSuggestionRejectedEvent
        {
            SuggestionId = Id,
            BimElementId = BimElementId,
            RejectedBy = rejectedBy,
            Reason = reason
        });
    }
}

public sealed class DerivedItemSuggestion
{
    public string DerivedCommodityCode { get; init; } = default!;
    public string? DerivedPricingCode { get; init; }
    public string QuantityFormula { get; init; } = default!;
    public string QuantityUnit { get; init; } = default!;
}

public enum SuggestionStatus
{
    Pending,
    Approved,
    Rejected
}
