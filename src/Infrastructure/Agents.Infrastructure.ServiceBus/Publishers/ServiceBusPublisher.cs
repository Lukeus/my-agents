using System.Text;
using System.Text.Json;
using Agents.Domain.Core.Events;
using Agents.Domain.Core.Interfaces;
using Agents.Infrastructure.ServiceBus.Configuration;
using Agents.Shared.Common.Serialization;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Agents.Infrastructure.ServiceBus.Publishers;

/// <summary>
/// Publishes domain events to Azure Service Bus with retry policies.
/// </summary>
public class ServiceBusPublisher : IEventPublisher, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;
    private readonly ServiceBusOptions _options;
    private readonly ILogger<ServiceBusPublisher> _logger;

    public ServiceBusPublisher(
        IOptions<ServiceBusOptions> options,
        ILogger<ServiceBusPublisher> logger)
    {
        _options = options.Value;
        _logger = logger;

        var clientOptions = new ServiceBusClientOptions
        {
            RetryOptions = new ServiceBusRetryOptions
            {
                Mode = _options.UseExponentialBackoff 
                    ? ServiceBusRetryMode.Exponential 
                    : ServiceBusRetryMode.Fixed,
                MaxRetries = _options.MaxRetryAttempts,
                Delay = TimeSpan.FromMilliseconds(_options.RetryDelayMilliseconds)
            }
        };

        _client = new ServiceBusClient(_options.ConnectionString, clientOptions);

        // Use queue or topic based on configuration
        var destination = _options.QueueName ?? _options.TopicName;
        if (string.IsNullOrEmpty(destination))
        {
            throw new InvalidOperationException("Either QueueName or TopicName must be configured");
        }

        _sender = _client.CreateSender(destination);

        _logger.LogInformation("Service Bus Publisher initialized for {Destination}", destination);
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
            using var messageBatch = await _sender.CreateMessageBatchAsync(cancellationToken);

            foreach (var domainEvent in events)
            {
                var message = CreateServiceBusMessage(domainEvent);

                if (!messageBatch.TryAddMessage(message))
                {
                    // Batch is full, send it
                    await _sender.SendMessagesAsync(messageBatch, cancellationToken);
                    _logger.LogInformation("Sent batch of messages to Service Bus");

                    // Create new batch with current message
                    using var newBatch = await _sender.CreateMessageBatchAsync(cancellationToken);
                    if (!newBatch.TryAddMessage(message))
                    {
                        throw new InvalidOperationException("Message is too large to fit in a batch");
                    }
                }
            }

            if (messageBatch.Count > 0)
            {
                await _sender.SendMessagesAsync(messageBatch, cancellationToken);
                _logger.LogInformation(
                    "Published {Count} messages to Service Bus",
                    events.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish messages to Service Bus");
            throw;
        }
    }

    private ServiceBusMessage CreateServiceBusMessage(IDomainEvent domainEvent)
    {
        var json = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), JsonDefaults.Options);
        var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(json))
        {
            MessageId = domainEvent.EventId.ToString(),
            CorrelationId = domainEvent.CorrelationId.ToString(),
            ContentType = "application/json"
        };

        // Add metadata as properties
        message.ApplicationProperties.Add("EventType", domainEvent.GetType().Name);
        message.ApplicationProperties.Add("OccurredAt", domainEvent.OccurredAt.ToString("O"));
        
        if (domainEvent.CausationId.HasValue)
        {
            message.ApplicationProperties.Add("CausationId", domainEvent.CausationId.Value.ToString());
        }

        // Support sessions if enabled
        if (_options.EnableSessions)
        {
            message.SessionId = domainEvent.CorrelationId.ToString();
        }

        return message;
    }

    public async ValueTask DisposeAsync()
    {
        await _sender.DisposeAsync();
        await _client.DisposeAsync();
        _logger.LogInformation("Service Bus Publisher disposed");
    }
}
