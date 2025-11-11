using System.Text;
using System.Text.Json;
using Agents.Domain.Core.Events;
using Agents.Domain.Core.Interfaces;
using Agents.Infrastructure.EventHub.Configuration;
using Agents.Shared.Common.Serialization;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Processor;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Agents.Infrastructure.EventHub.Consumers;

/// <summary>
/// Consumes events from Azure Event Hubs with checkpointing.
/// </summary>
public class EventHubConsumer : IAsyncDisposable
{
    private readonly EventProcessorClient _processorClient;
    private readonly EventHubOptions _options;
    private readonly ILogger<EventHubConsumer> _logger;
    private readonly Dictionary<string, Type> _eventTypeRegistry = new();
    private readonly Dictionary<Type, object> _handlers = new();

    public EventHubConsumer(
        IOptions<EventHubOptions> options,
        ILogger<EventHubConsumer> logger)
    {
        _options = options.Value;
        _logger = logger;

        var storageClient = new BlobContainerClient(
            _options.BlobStorageConnectionString,
            _options.CheckpointContainerName);

        _processorClient = new EventProcessorClient(
            storageClient,
            _options.ConsumerGroup,
            _options.ConnectionString,
            _options.EventHubName);

        _processorClient.ProcessEventAsync += ProcessEventHandlerAsync;
        _processorClient.ProcessErrorAsync += ProcessErrorHandlerAsync;

        _logger.LogInformation(
            "Event Hub Consumer initialized for {EventHubName} with consumer group {ConsumerGroup}",
            _options.EventHubName,
            _options.ConsumerGroup);
    }

    /// <summary>
    /// Registers an event type with its corresponding CLR type.
    /// </summary>
    public void RegisterEventType<TEvent>(string eventTypeName) where TEvent : IDomainEvent
    {
        _eventTypeRegistry[eventTypeName] = typeof(TEvent);
        _logger.LogInformation("Registered event type {EventType} -> {ClrType}", eventTypeName, typeof(TEvent).Name);
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
    /// Starts processing events from the Event Hub.
    /// </summary>
    public async Task StartProcessingAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Event Hub Consumer");
        await _processorClient.StartProcessingAsync(cancellationToken);
    }

    /// <summary>
    /// Stops processing events from the Event Hub.
    /// </summary>
    public async Task StopProcessingAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping Event Hub Consumer");
        await _processorClient.StopProcessingAsync(cancellationToken);
    }

    private async Task ProcessEventHandlerAsync(ProcessEventArgs args)
    {
        try
        {
            if (args.Data.EventBody.Length == 0)
            {
                return;
            }

            var eventTypeName = args.Data.Properties.TryGetValue("EventType", out var type)
                ? type.ToString()
                : null;

            if (string.IsNullOrEmpty(eventTypeName) || !_eventTypeRegistry.TryGetValue(eventTypeName, out var clrType))
            {
                _logger.LogWarning(
                    "No CLR type registered for event type {EventType}. Skipping.",
                    eventTypeName);

                if (_options.EnableAutoCheckpoint)
                {
                    await args.UpdateCheckpointAsync(args.CancellationToken);
                }
                return;
            }

            var json = Encoding.UTF8.GetString(args.Data.EventBody.ToArray());
            var domainEvent = JsonSerializer.Deserialize(json, clrType, JsonDefaults.Options) as IDomainEvent;

            if (domainEvent == null)
            {
                _logger.LogWarning("Failed to deserialize event of type {EventType}", eventTypeName);

                if (_options.EnableAutoCheckpoint)
                {
                    await args.UpdateCheckpointAsync(args.CancellationToken);
                }
                return;
            }

            if (!_handlers.TryGetValue(clrType, out var handler))
            {
                _logger.LogWarning("No handler registered for event type {EventType}", clrType.Name);

                if (_options.EnableAutoCheckpoint)
                {
                    await args.UpdateCheckpointAsync(args.CancellationToken);
                }
                return;
            }

            _logger.LogInformation(
                "Processing event {EventId} of type {EventType} from partition {PartitionId}",
                domainEvent.EventId,
                clrType.Name,
                args.Partition.PartitionId);

            // Invoke handler using reflection
            var handleMethod = handler.GetType().GetMethod("HandleAsync");
            if (handleMethod != null)
            {
                var task = handleMethod.Invoke(handler, new object[] { domainEvent, args.CancellationToken }) as Task;
                if (task != null)
                {
                    await task;
                }
            }

            _logger.LogInformation("Successfully processed event {EventId}", domainEvent.EventId);

            // Update checkpoint after successful processing
            if (_options.EnableAutoCheckpoint)
            {
                await args.UpdateCheckpointAsync(args.CancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing event from partition {PartitionId}",
                args.Partition.PartitionId);

            // Don't checkpoint on error - event will be reprocessed
        }
    }

    private Task ProcessErrorHandlerAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(
            args.Exception,
            "Error in Event Hub processor for partition {PartitionId}: {Operation}",
            args.PartitionId,
            args.Operation);

        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await _processorClient.StopProcessingAsync();
        _logger.LogInformation("Event Hub Consumer disposed");
    }
}
