using Agents.Domain.Core.Events;
using Microsoft.Azure.Cosmos;

namespace Agents.Infrastructure.Persistence.CosmosDB.EventStore;

/// <summary>
/// Event store implementation using Cosmos DB.
/// </summary>
public class EventStore : IEventStore
{
    private readonly Container _container;

    public EventStore(Container container)
    {
        _container = container ?? throw new ArgumentNullException(nameof(container));
    }

    /// <summary>
    /// Saves an event to the event store.
    /// </summary>
    public async Task SaveEventAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : DomainEvent
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var eventDocument = new EventDocument
        {
            Id = domainEvent.EventId.ToString(),
            EventType = domainEvent.GetType().Name,
            AggregateId = domainEvent.CorrelationId.ToString(),
            Timestamp = domainEvent.OccurredAt,
            Data = System.Text.Json.JsonSerializer.Serialize(domainEvent),
            Version = 1
        };

        await _container.CreateItemAsync(
            eventDocument,
            new PartitionKey(eventDocument.AggregateId),
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Retrieves events for a specific aggregate.
    /// </summary>
    public async Task<IEnumerable<TEvent>> GetEventsAsync<TEvent>(string aggregateId, CancellationToken cancellationToken = default)
        where TEvent : DomainEvent
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.aggregateId = @aggregateId ORDER BY c.timestamp ASC")
            .WithParameter("@aggregateId", aggregateId);

        var iterator = _container.GetItemQueryIterator<EventDocument>(
            query,
            requestOptions: new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(aggregateId)
            });

        var events = new List<TEvent>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);

            foreach (var eventDoc in response)
            {
                var domainEvent = System.Text.Json.JsonSerializer.Deserialize<TEvent>(eventDoc.Data);
                if (domainEvent != null)
                {
                    events.Add(domainEvent);
                }
            }
        }

        return events;
    }
}

/// <summary>
/// Interface for event store operations.
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// Saves a domain event to the event store.
    /// </summary>
    Task SaveEventAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : DomainEvent;

    /// <summary>
    /// Retrieves all events for a specific aggregate.
    /// </summary>
    Task<IEnumerable<TEvent>> GetEventsAsync<TEvent>(string aggregateId, CancellationToken cancellationToken = default)
        where TEvent : DomainEvent;
}

/// <summary>
/// Document model for storing events in Cosmos DB.
/// </summary>
internal class EventDocument
{
    public string Id { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string AggregateId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Data { get; set; } = string.Empty;
    public int Version { get; set; }
}
