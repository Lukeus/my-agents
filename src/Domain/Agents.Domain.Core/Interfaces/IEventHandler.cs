using Agents.Domain.Core.Events;

namespace Agents.Domain.Core.Interfaces;

/// <summary>
/// Interface for handling domain events.
/// </summary>
/// <typeparam name="TEvent">The type of domain event to handle.</typeparam>
public interface IEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    /// <summary>
    /// Handles the specified domain event.
    /// </summary>
    /// <param name="domainEvent">The domain event to handle.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
