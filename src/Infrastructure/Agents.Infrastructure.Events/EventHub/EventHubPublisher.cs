using System.Text.Json;
using Agents.Domain.Core.Events;
using Agents.Domain.Core.Interfaces;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Agents.Infrastructure.Events.EventHub;

/// <summary>
/// Publishes domain events to Azure Event Hub
/// </summary>
public class EventHubPublisher : IEventPublisher, IAsyncDisposable
{
    private readonly EventHubProducerClient _producerClient;
    private readonly ILogger<EventHubPublisher> _logger;
    private readonly string _eventHubName;

    public EventHubPublisher(
        IConfiguration configuration,
        ILogger<EventHubPublisher> logger)
    {
        _logger = logger;

        var connectionString = configuration["EventHub:ConnectionString"]
            ?? throw new InvalidOperationException("EventHub:ConnectionString is not configured");

        _eventHubName = configuration["EventHub:EventHubName"]
            ?? throw new InvalidOperationException("EventHub:EventHubName is not configured");

        _producerClient = new EventHubProducerClient(connectionString, _eventHubName);
    }

    public async Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            using var eventBatch = await _producerClient.CreateBatchAsync(cancellationToken);

            var eventData = CreateEventData(domainEvent);

            if (!eventBatch.TryAdd(eventData))
            {
                throw new InvalidOperationException($"Event is too large for the batch and cannot be sent.");
            }

            await _producerClient.SendAsync(eventBatch, cancellationToken);

            _logger.LogInformation(
                "Published event {EventType} with ID {EventId} to Event Hub {EventHubName}",
                domainEvent.GetType().Name,
                domainEvent.EventId,
                _eventHubName);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish event {EventType} with ID {EventId} to Event Hub",
                domainEvent.GetType().Name,
                domainEvent.EventId);
            throw;
        }
    }

    public async Task PublishAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        var events = domainEvents.ToList();
        if (!events.Any())
            return;

        try
        {
            using var eventBatch = await _producerClient.CreateBatchAsync(cancellationToken);

            foreach (var domainEvent in events)
            {
                var eventData = CreateEventData(domainEvent);

                if (!eventBatch.TryAdd(eventData))
                {
                    // If we can't fit this event, send what we have and create a new batch
                    await _producerClient.SendAsync(eventBatch, cancellationToken);

                    // Create new batch since we can't clear it
                    using var newBatch = await _producerClient.CreateBatchAsync(cancellationToken);
                    if (!newBatch.TryAdd(eventData))
                    {
                        _logger.LogWarning(
                            "Event {EventType} with ID {EventId} is too large and will be skipped",
                            domainEvent.GetType().Name,
                            domainEvent.EventId);
                    }
                    else
                    {
                        await _producerClient.SendAsync(newBatch, cancellationToken);
                    }
                }
            }

            if (eventBatch.Count > 0)
            {
                await _producerClient.SendAsync(eventBatch, cancellationToken);
            }

            _logger.LogInformation(
                "Published {EventCount} events to Event Hub {EventHubName}",
                events.Count,
                _eventHubName);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish {EventCount} events to Event Hub",
                events.Count);
            throw;
        }
    }

    private EventData CreateEventData(IDomainEvent domainEvent)
    {
        var eventJson = JsonSerializer.Serialize(domainEvent, domainEvent.GetType());
        var eventData = new EventData(eventJson);

        // Add metadata as properties
        eventData.Properties.Add("EventType", domainEvent.GetType().Name);
        eventData.Properties.Add("EventId", domainEvent.EventId.ToString());
        eventData.Properties.Add("OccurredAt", domainEvent.OccurredAt);
        eventData.ContentType = "application/json";

        return eventData;
    }

    public async ValueTask DisposeAsync()
    {
        await _producerClient.DisposeAsync();
    }
}
