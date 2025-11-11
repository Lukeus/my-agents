using System.Text.Json;
using Agents.Domain.Core.Events;
using Agents.Domain.Core.Interfaces;
using Agents.Infrastructure.EventGrid.Configuration;
using Agents.Infrastructure.EventGrid.Models;
using Agents.Shared.Common.Serialization;
using Azure;
using Azure.Messaging.EventGrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Agents.Infrastructure.EventGrid.Publishers;

/// <summary>
/// Publishes domain events to Azure Event Grid.
/// </summary>
public class EventGridPublisher : IEventPublisher
{
    private readonly EventGridPublisherClient _client;
    private readonly EventGridOptions _options;
    private readonly ILogger<EventGridPublisher> _logger;

    public EventGridPublisher(
        IOptions<EventGridOptions> options,
        ILogger<EventGridPublisher> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (_options.UseEmulator)
        {
            _logger.LogInformation("Using Event Grid emulator at {Endpoint}", _options.Endpoint);
            _client = new EventGridPublisherClient(new Uri(_options.Endpoint), new AzureKeyCredential("emulator-key"));
        }
        else if (!string.IsNullOrEmpty(_options.AccessKey))
        {
            _client = new EventGridPublisherClient(
                new Uri(_options.Endpoint),
                new AzureKeyCredential(_options.AccessKey));
        }
        else
        {
            throw new InvalidOperationException("Event Grid AccessKey is required when not using emulator");
        }
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
            var eventGridEvents = events.Select(CreateEventGridEvent).ToList();

            _logger.LogInformation(
                "Publishing {Count} events to Event Grid at {Endpoint}",
                events.Count,
                _options.Endpoint);

            await RetryAsync(async () =>
            {
                await _client.SendEventsAsync(eventGridEvents, cancellationToken);
            });

            _logger.LogInformation("Successfully published {Count} events", events.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish events to Event Grid");

            if (_options.EnableDeadLetter)
            {
                await SendToDeadLetterAsync(events, ex);
            }

            throw;
        }
    }

    private EventGridEvent CreateEventGridEvent(IDomainEvent domainEvent)
    {
        var eventType = GetEventType(domainEvent.GetType());
        var subject = $"agents/{eventType}";

        var dataJson = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), JsonDefaults.Options);

        return new EventGridEvent(
            subject: subject,
            eventType: eventType,
            dataVersion: "1.0",
            data: new BinaryData(dataJson));
    }

    private string GetEventType(Type eventType)
    {
        // Extract event type from namespace
        // e.g., Agents.Domain.Notification.Events.NotificationSentEvent -> agents.notification.sent
        var namespaceParts = eventType.Namespace?.Split('.') ?? Array.Empty<string>();
        var domain = namespaceParts.Length >= 3 ? namespaceParts[2].ToLowerInvariant() : "unknown";

        var name = eventType.Name;
        if (name.EndsWith("Event", StringComparison.OrdinalIgnoreCase))
        {
            name = name[..^5];
        }

        var action = string.Concat(name.Select((x, i) =>
            i > 0 && char.IsUpper(x) ? "." + char.ToLower(x) : char.ToLower(x).ToString()));

        return $"agents.{domain}.{action}";
    }

    private async Task RetryAsync(Func<Task> operation)
    {
        var attempt = 0;
        var delay = _options.RetryDelayMilliseconds;

        while (attempt < _options.MaxRetryAttempts)
        {
            try
            {
                await operation();
                return;
            }
            catch (Exception ex) when (attempt < _options.MaxRetryAttempts - 1)
            {
                attempt++;
                _logger.LogWarning(
                    ex,
                    "Retry attempt {Attempt} of {MaxAttempts} after {Delay}ms",
                    attempt,
                    _options.MaxRetryAttempts,
                    delay);

                await Task.Delay(delay);
                delay *= 2; // Exponential backoff
            }
        }
    }

    private async Task SendToDeadLetterAsync(List<IDomainEvent> events, Exception exception)
    {
        try
        {
            // TODO: Implement dead letter storage (e.g., to Blob Storage or dedicated queue)
            _logger.LogWarning(
                "Sending {Count} failed events to dead letter queue. Error: {Error}",
                events.Count,
                exception.Message);

            // Placeholder for dead letter implementation
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send events to dead letter queue");
        }
    }
}
