using System.Text.Json;
using Agents.Domain.Core.Events;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Agents.Infrastructure.Events.ServiceBus;

/// <summary>
/// Background service that consumes messages from Azure Service Bus
/// </summary>
public class ServiceBusConsumer : BackgroundService
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusProcessor _processor;
    private readonly ILogger<ServiceBusConsumer> _logger;
    private readonly string _topicName;
    private readonly string _subscriptionName;

    public ServiceBusConsumer(
        IConfiguration configuration,
        ILogger<ServiceBusConsumer> logger,
        string topicName,
        string subscriptionName)
    {
        _logger = logger;
        _topicName = topicName;
        _subscriptionName = subscriptionName;

        var connectionString = configuration["ServiceBus:ConnectionString"]
            ?? throw new InvalidOperationException("ServiceBus:ConnectionString is not configured");

        _client = new ServiceBusClient(connectionString);

        _processor = _client.CreateProcessor(_topicName, _subscriptionName, new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = 10,
            AutoCompleteMessages = false,
            ReceiveMode = ServiceBusReceiveMode.PeekLock
        });

        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Starting Service Bus consumer for topic {TopicName}, subscription {SubscriptionName}",
            _topicName,
            _subscriptionName);

        await _processor.StartProcessingAsync(stoppingToken);

        // Keep running until cancellation
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        var messageId = args.Message.MessageId;
        var eventType = args.Message.ApplicationProperties.ContainsKey("EventType")
            ? args.Message.ApplicationProperties["EventType"]?.ToString()
            : "Unknown";

        try
        {
            _logger.LogInformation(
                "Processing message {MessageId} of type {EventType} from topic {TopicName}",
                messageId,
                eventType,
                _topicName);

            var body = args.Message.Body.ToString();

            // Deserialize and process the event
            // This is where you would dispatch to handlers based on event type
            await ProcessEventAsync(body, eventType, args.CancellationToken);

            // Complete the message
            await args.CompleteMessageAsync(args.Message, args.CancellationToken);

            _logger.LogInformation(
                "Completed processing message {MessageId} of type {EventType}",
                messageId,
                eventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing message {MessageId} of type {EventType}",
                messageId,
                eventType);

            // Abandon the message so it can be retried
            await args.AbandonMessageAsync(args.Message, cancellationToken: args.CancellationToken);
        }
    }

    private Task ProcessEventAsync(string eventJson, string? eventType, CancellationToken cancellationToken)
    {
        // TODO: Implement event dispatching logic
        // This would typically involve:
        // 1. Deserializing to the appropriate event type
        // 2. Calling registered handlers
        // 3. Publishing to internal event bus

        _logger.LogDebug("Processing event of type {EventType}: {EventJson}", eventType, eventJson);

        return Task.CompletedTask;
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(
            args.Exception,
            "Error processing Service Bus message. Error source: {ErrorSource}, Entity path: {EntityPath}",
            args.ErrorSource,
            args.EntityPath);

        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Stopping Service Bus consumer for topic {TopicName}, subscription {SubscriptionName}",
            _topicName,
            _subscriptionName);

        await _processor.StopProcessingAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _processor.DisposeAsync().AsTask().Wait();
        _client.DisposeAsync().AsTask().Wait();
        base.Dispose();
    }
}
