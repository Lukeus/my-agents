using System.Text.Json;
using Agents.Domain.Core.Events;
using Agents.Domain.Core.Interfaces;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Agents.Infrastructure.Events.ServiceBus;

/// <summary>
/// Publishes domain events to Azure Service Bus topics
/// </summary>
public class ServiceBusPublisher : IEventPublisher, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly Dictionary<string, ServiceBusSender> _senders;
    private readonly ILogger<ServiceBusPublisher> _logger;
    private readonly IConfiguration _configuration;

    public ServiceBusPublisher(
        IConfiguration configuration,
        ILogger<ServiceBusPublisher> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _senders = new Dictionary<string, ServiceBusSender>();

        var connectionString = configuration["ServiceBus:ConnectionString"]
            ?? throw new InvalidOperationException("ServiceBus:ConnectionString is not configured");

        _client = new ServiceBusClient(connectionString);
    }

    public async Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            var topicName = GetTopicName(domainEvent);
            var sender = GetOrCreateSender(topicName);

            var message = CreateMessage(domainEvent);
            await sender.SendMessageAsync(message, cancellationToken);

            _logger.LogInformation(
                "Published event {EventType} with ID {EventId} to Service Bus topic {TopicName}",
                domainEvent.GetType().Name,
                domainEvent.EventId,
                topicName);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish event {EventType} with ID {EventId} to Service Bus",
                domainEvent.GetType().Name,
                domainEvent.EventId);
            throw;
        }
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
            // Group events by topic
            var eventsByTopic = events.GroupBy(e => GetTopicName(e));

            foreach (var topicGroup in eventsByTopic)
            {
                var sender = GetOrCreateSender(topicGroup.Key);
                var messages = topicGroup.Select(CreateMessage).ToList();

                await sender.SendMessagesAsync(messages, cancellationToken);

                _logger.LogInformation(
                    "Published {EventCount} events to Service Bus topic {TopicName}",
                    messages.Count,
                    topicGroup.Key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish {EventCount} events to Service Bus",
                events.Count);
            throw;
        }
    }

    private ServiceBusMessage CreateMessage(IDomainEvent domainEvent)
    {
        var eventJson = JsonSerializer.Serialize(domainEvent, domainEvent.GetType());
        var message = new ServiceBusMessage(eventJson)
        {
            ContentType = "application/json",
            Subject = domainEvent.GetType().Name,
            MessageId = domainEvent.EventId.ToString(),
            CorrelationId = domainEvent.CorrelationId.ToString()
        };

        // Add custom properties
        message.ApplicationProperties.Add("EventType", domainEvent.GetType().Name);
        message.ApplicationProperties.Add("OccurredAt", domainEvent.OccurredAt);

        return message;
    }

    private string GetTopicName(IDomainEvent domainEvent)
    {
        // Map event types to topics based on configuration or convention
        var eventType = domainEvent.GetType().Name;

        if (eventType.Contains("Notification"))
        {
            return _configuration["ServiceBus:NotificationTopic"] ?? "notification-events";
        }

        if (eventType.Contains("DevOps"))
        {
            return _configuration["ServiceBus:DevOpsTopic"] ?? "devops-events";
        }

        // Default topic
        return "agent-events";
    }

    private ServiceBusSender GetOrCreateSender(string topicName)
    {
        if (!_senders.ContainsKey(topicName))
        {
            _senders[topicName] = _client.CreateSender(topicName);
        }

        return _senders[topicName];
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var sender in _senders.Values)
        {
            await sender.DisposeAsync();
        }

        await _client.DisposeAsync();
    }
}
