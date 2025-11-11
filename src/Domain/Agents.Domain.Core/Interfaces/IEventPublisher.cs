using Agents.Domain.Core.Events;

namespace Agents.Domain.Core.Interfaces;

/// <summary>
/// Interface for publishing domain events to the event bus.
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes a single domain event.
    /// </summary>
    /// <param name="domainEvent">The domain event to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes multiple domain events.
    /// </summary>
    /// <param name="domainEvents">The domain events to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
