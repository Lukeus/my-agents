using Agents.Domain.Core.Events;

namespace Agents.Domain.BimClassification.Events;

/// <summary>
/// Event raised when a classification suggestion is generated.
/// </summary>
public class ClassificationSuggestedEvent : DomainEvent
{
    /// <summary>
    /// Gets the BIM element ID.
    /// </summary>
    public required long BimElementId { get; init; }

    /// <summary>
    /// Gets the suggestion ID.
    /// </summary>
    public required long SuggestionId { get; init; }

    /// <summary>
    /// Gets the suggested commodity code.
    /// </summary>
    public string? CommodityCode { get; init; }
}

/// <summary>
/// Event raised when a classification suggestion is approved.
/// </summary>
public class ClassificationSuggestionApprovedEvent : DomainEvent
{
    /// <summary>
    /// Gets the suggestion ID.
    /// </summary>
    public required long SuggestionId { get; init; }

    /// <summary>
    /// Gets the BIM element ID.
    /// </summary>
    public required long BimElementId { get; init; }

    /// <summary>
    /// Gets the identifier of the user who approved the suggestion.
    /// </summary>
    public required string ApprovedBy { get; init; }
}

/// <summary>
/// Event raised when a classification suggestion is rejected.
/// </summary>
public class ClassificationSuggestionRejectedEvent : DomainEvent
{
    /// <summary>
    /// Gets the suggestion ID.
    /// </summary>
    public required long SuggestionId { get; init; }

    /// <summary>
    /// Gets the BIM element ID.
    /// </summary>
    public required long BimElementId { get; init; }

    /// <summary>
    /// Gets the identifier of the user who rejected the suggestion.
    /// </summary>
    public required string RejectedBy { get; init; }

    /// <summary>
    /// Gets the reason for rejection.
    /// </summary>
    public required string Reason { get; init; }
}
