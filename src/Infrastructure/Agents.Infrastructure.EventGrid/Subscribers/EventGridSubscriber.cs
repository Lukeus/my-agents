using System.Text.Json;
using Agents.Domain.Core.Events;
using Agents.Domain.Core.Interfaces;
using Agents.Shared.Common.Serialization;
using Azure.Messaging.EventGrid;
using Microsoft.Extensions.Logging;

namespace Agents.Infrastructure.EventGrid.Subscribers;

/// <summary>
/// Processes events received from Azure Event Grid.
/// </summary>
public class EventGridSubscriber
{
    private readonly ILogger<EventGridSubscriber> _logger;
    private readonly Dictionary<string, Type> _eventTypeRegistry = new();
    private readonly Dictionary<Type, object> _handlers = new();

    public EventGridSubscriber(ILogger<EventGridSubscriber> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Registers an event type with its corresponding CLR type.
    /// </summary>
    public void RegisterEventType<TEvent>(string eventType) where TEvent : IDomainEvent
    {
        _eventTypeRegistry[eventType] = typeof(TEvent);
        _logger.LogInformation("Registered event type {EventType} -> {ClrType}", eventType, typeof(TEvent).Name);
    }

    /// <summary>
    /// Registers an event handler for a specific event type.
    /// </summary>
    public void RegisterHandler<TEvent>(IEventHandler<TEvent> handler) where TEvent : IDomainEvent
    {
        _handlers[typeof(TEvent)] = handler;
        _logger.LogInformation("Registered handler for {EventType}", typeof(TEvent).Name);
    }

    /// <summary>
    /// Processes incoming Event Grid events.
    /// </summary>
    public async Task ProcessEventsAsync(
        EventGridEvent[] events,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing {Count} Event Grid events", events.Length);

        foreach (var eventGridEvent in events)
        {
            try
            {
                await ProcessEventAsync(eventGridEvent, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to process event {EventId} of type {EventType}",
                    eventGridEvent.Id,
                    eventGridEvent.EventType);
            }
        }
    }

    private async Task ProcessEventAsync(EventGridEvent eventGridEvent, CancellationToken cancellationToken)
    {
        if (!_eventTypeRegistry.TryGetValue(eventGridEvent.EventType, out var clrType))
        {
            _logger.LogWarning(
                "No CLR type registered for Event Grid event type {EventType}",
                eventGridEvent.EventType);
            return;
        }

        var dataJson = eventGridEvent.Data.ToString();
        var domainEvent = JsonSerializer.Deserialize(dataJson!, clrType, JsonDefaults.Options) as IDomainEvent;

        if (domainEvent == null)
        {
            _logger.LogWarning(
                "Failed to deserialize event {EventId} to type {ClrType}",
                eventGridEvent.Id,
                clrType.Name);
            return;
        }

        if (!_handlers.TryGetValue(clrType, out var handler))
        {
            _logger.LogWarning(
                "No handler registered for event type {EventType}",
                clrType.Name);
            return;
        }

        _logger.LogInformation(
            "Invoking handler for event {EventId} of type {EventType}",
            domainEvent.EventId,
            clrType.Name);

        // Use reflection to invoke the HandleAsync method
        var handleMethod = handler.GetType().GetMethod("HandleAsync");
        if (handleMethod != null)
        {
            var task = handleMethod.Invoke(handler, new object[] { domainEvent, cancellationToken }) as Task;
            if (task != null)
            {
                await task;
            }
        }

        _logger.LogInformation(
            "Successfully processed event {EventId}",
            domainEvent.EventId);
    }
}
