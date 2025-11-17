using Agents.Domain.BimClassification.Events;
using Agents.Domain.Core.Entities;

namespace Agents.Domain.BimClassification.Entities;

/// <summary>
/// Aggregate root representing an agent-generated classification suggestion.
/// This is ADVISORY ONLY - never the canonical classification.
/// </summary>
public sealed class BimClassificationSuggestion : AggregateRoot<long>
{
    /// <summary>
    /// Gets the BIM element ID that this suggestion applies to.
    /// </summary>
    public long BimElementId { get; private set; }

    /// <summary>
    /// Gets the suggested commodity code for the BIM element.
    /// </summary>
    public string? SuggestedCommodityCode { get; private set; }

    /// <summary>
    /// Gets the suggested pricing code for the BIM element.
    /// </summary>
    public string? SuggestedPricingCode { get; private set; }

    /// <summary>
    /// Gets the list of derived item suggestions for this classification.
    /// </summary>
    public List<DerivedItemSuggestion> DerivedItems { get; private set; } = new();

    /// <summary>
    /// Gets the AI-generated reasoning summary explaining the classification decision.
    /// </summary>
    public string ReasoningSummary { get; private set; } = default!;

    /// <summary>
    /// Gets the current status of the suggestion (Pending, Approved, Rejected).
    /// </summary>
    public SuggestionStatus Status { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the suggestion was created.
    /// </summary>
    public DateTime CreatedUtc { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the suggestion was reviewed.
    /// </summary>
    public DateTime? ReviewedUtc { get; private set; }

    /// <summary>
    /// Gets the identifier of the user who reviewed the suggestion.
    /// </summary>
    public string? ReviewedBy { get; private set; }

    private BimClassificationSuggestion() : base(0) { }  // EF Core

    /// <summary>
    /// Creates a new classification suggestion for a BIM element.
    /// </summary>
    /// <param name="bimElementId">The BIM element ID.</param>
    /// <param name="commodityCode">The suggested commodity code.</param>
    /// <param name="pricingCode">The suggested pricing code.</param>
    /// <param name="derivedItems">The derived item suggestions.</param>
    /// <param name="reasoningSummary">The reasoning summary for the classification.</param>
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

    /// <summary>
    /// Approves the classification suggestion.
    /// </summary>
    /// <param name="approvedBy">The identifier of the user approving the suggestion.</param>
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

    /// <summary>
    /// Rejects the classification suggestion.
    /// </summary>
    /// <param name="rejectedBy">The identifier of the user rejecting the suggestion.</param>
    /// <param name="reason">The reason for rejection.</param>
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

/// <summary>
/// Represents a suggested derived item from a BIM element.
/// </summary>
public sealed class DerivedItemSuggestion
{
    /// <summary>
    /// Gets or initializes the commodity code for the derived item.
    /// </summary>
    public string DerivedCommodityCode { get; init; } = default!;

    /// <summary>
    /// Gets or initializes the pricing code for the derived item.
    /// </summary>
    public string? DerivedPricingCode { get; init; }

    /// <summary>
    /// Gets or initializes the formula for calculating the quantity.
    /// </summary>
    public string QuantityFormula { get; init; } = default!;

    /// <summary>
    /// Gets or initializes the unit of measurement for the quantity.
    /// </summary>
    public string QuantityUnit { get; init; } = default!;
}

/// <summary>
/// Represents the status of a classification suggestion.
/// </summary>
public enum SuggestionStatus
{
    /// <summary>
    /// The suggestion is pending review.
    /// </summary>
    Pending,

    /// <summary>
    /// The suggestion has been approved.
    /// </summary>
    Approved,

    /// <summary>
    /// The suggestion has been rejected.
    /// </summary>
    Rejected
}
