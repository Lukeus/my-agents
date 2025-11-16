using Agents.Domain.Core.Events;

namespace Agents.Domain.BimClassification.Events;

/// <summary>
/// Event raised when a classification suggestion is generated
/// </summary>
public class ClassificationSuggestedEvent : DomainEvent
{
    public required long BimElementId { get; init; }
    public required long SuggestionId { get; init; }
    public string? CommodityCode { get; init; }
}

/// <summary>
/// Event raised when a classification suggestion is approved
/// </summary>
public class ClassificationSuggestionApprovedEvent : DomainEvent
{
    public required long SuggestionId { get; init; }
    public required long BimElementId { get; init; }
    public required string ApprovedBy { get; init; }
}

/// <summary>
/// Event raised when a classification suggestion is rejected
/// </summary>
public class ClassificationSuggestionRejectedEvent : DomainEvent
{
    public required long SuggestionId { get; init; }
    public required long BimElementId { get; init; }
    public required string RejectedBy { get; init; }
    public required string Reason { get; init; }
}
