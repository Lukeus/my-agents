namespace Agents.Domain.Core.Events;

/// <summary>
/// Base class for domain events.
/// </summary>
public abstract class DomainEvent : IDomainEvent
{
    /// <inheritdoc />
    public Guid EventId { get; }

    /// <inheritdoc />
    public DateTime OccurredAt { get; }

    /// <inheritdoc />
    public Guid CorrelationId { get; }

    /// <inheritdoc />
    public Guid? CausationId { get; }

    protected DomainEvent(Guid? correlationId = null, Guid? causationId = null)
    {
        EventId = Guid.NewGuid();
        OccurredAt = DateTime.UtcNow;
        CorrelationId = correlationId ?? Guid.NewGuid();
        CausationId = causationId;
    }
}
