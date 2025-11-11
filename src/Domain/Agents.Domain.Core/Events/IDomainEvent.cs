namespace Agents.Domain.Core.Events;

/// <summary>
/// Marker interface for domain events.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Gets the unique identifier of the event.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Gets the timestamp when the event occurred.
    /// </summary>
    DateTime OccurredAt { get; }

    /// <summary>
    /// Gets the correlation ID for tracking related events.
    /// </summary>
    Guid CorrelationId { get; }

    /// <summary>
    /// Gets the causation ID (the event that caused this event).
    /// </summary>
    Guid? CausationId { get; }
}
