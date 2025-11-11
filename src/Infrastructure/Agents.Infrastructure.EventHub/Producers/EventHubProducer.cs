using System.Text;
using System.Text.Json;
using Agents.Domain.Core.Events;
using Agents.Domain.Core.Interfaces;
using Agents.Infrastructure.EventHub.Configuration;
using Agents.Shared.Common.Serialization;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Agents.Infrastructure.EventHub.Producers;

/// <summary>
/// Produces events to Azure Event Hubs with batching support.
/// </summary>
public class EventHubProducer : IEventPublisher, IAsyncDisposable
{
    private readonly EventHubProducerClient _producerClient;
    private readonly EventHubOptions _options;
    private readonly ILogger<EventHubProducer> _logger;

    public EventHubProducer(
        IOptions<EventHubOptions> options,
        ILogger<EventHubProducer> logger)
    {
        _options = options.Value;
        _logger = logger;

        _producerClient = new EventHubProducerClient(
            _options.ConnectionString,
            _options.EventHubName);

        _logger.LogInformation("Event Hub Producer initialized for {EventHubName}", _options.EventHubName);
    }

    public async Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        await PublishAsync(new[] { domainEvent }, cancellationToken);
    }

    public async Task PublishAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        var events = domainEvents.ToList();
        if (!events.Any())
        {
            return;
        }

        try
        {
            using var eventBatch = await _producerClient.CreateBatchAsync(cancellationToken);

            foreach (var domainEvent in events)
            {
                var eventData = CreateEventData(domainEvent);

                if (!eventBatch.TryAdd(eventData))
                {
                    // Batch is full, send it and create a new one
                    await _producerClient.SendAsync(eventBatch, cancellationToken);

                    _logger.LogInformation("Sent batch of events to Event Hub");

                    // Start new batch with current event
                    using var newBatch = await _producerClient.CreateBatchAsync(cancellationToken);
                    if (!newBatch.TryAdd(eventData))
                    {
                        throw new InvalidOperationException("Event is too large to fit in a batch");
                    }
                }
            }

            if (eventBatch.Count > 0)
            {
                await _producerClient.SendAsync(eventBatch, cancellationToken);
                _logger.LogInformation("Published {Count} events to Event Hub {EventHubName}",
                    events.Count,
                    _options.EventHubName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish events to Event Hub");
            throw;
        }
    }

    private EventData CreateEventData(IDomainEvent domainEvent)
    {
        var json = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), JsonDefaults.Options);
        var eventData = new EventData(Encoding.UTF8.GetBytes(json));

        // Add metadata as properties
        eventData.Properties.Add("EventId", domainEvent.EventId.ToString());
        eventData.Properties.Add("EventType", domainEvent.GetType().Name);
        eventData.Properties.Add("CorrelationId", domainEvent.CorrelationId.ToString());

        if (domainEvent.CausationId.HasValue)
        {
            eventData.Properties.Add("CausationId", domainEvent.CausationId.Value.ToString());
        }

        eventData.Properties.Add("OccurredAt", domainEvent.OccurredAt.ToString("O"));

        return eventData;
    }

    public async ValueTask DisposeAsync()
    {
        await _producerClient.DisposeAsync();
        _logger.LogInformation("Event Hub Producer disposed");
    }
}
