using System.Text;
using System.Text.Json;
using Agents.Domain.Core.Events;
using Agents.Domain.Core.Interfaces;
using Agents.Infrastructure.ServiceBus.Configuration;
using Agents.Shared.Common.Serialization;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Agents.Infrastructure.ServiceBus.Subscribers;

/// <summary>
/// Subscribes to messages from Azure Service Bus with error handling.
/// </summary>
public class ServiceBusSubscriber : IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusProcessor? _processor;
    private readonly ServiceBusSessionProcessor? _sessionProcessor;
    private readonly ServiceBusOptions _options;
    private readonly ILogger<ServiceBusSubscriber> _logger;
    private readonly Dictionary<string, Type> _eventTypeRegistry = new();
    private readonly Dictionary<Type, object> _handlers = new();

    public ServiceBusSubscriber(
        IOptions<ServiceBusOptions> options,
        ILogger<ServiceBusSubscriber> logger)
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

        // Create processor for queue or topic/subscription
        var processorOptions = new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = _options.MaxConcurrentCalls,
            AutoCompleteMessages = false, // Manual completion for better control
            MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(_options.MaxAutoLockRenewalMinutes)
        };

        if (!string.IsNullOrEmpty(_options.QueueName))
        {
            if (_options.EnableSessions)
            {
                var sessionOptions = new ServiceBusSessionProcessorOptions
                {
                    MaxConcurrentSessions = _options.MaxConcurrentCalls,
                    AutoCompleteMessages = false,
                    MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(_options.MaxAutoLockRenewalMinutes)
                };
                _sessionProcessor = _client.CreateSessionProcessor(_options.QueueName, sessionOptions);
            }
            else
            {
                _processor = _client.CreateProcessor(_options.QueueName, processorOptions);
            }
        }
        else if (!string.IsNullOrEmpty(_options.TopicName) && !string.IsNullOrEmpty(_options.SubscriptionName))
        {
            if (_options.EnableSessions)
            {
                var sessionOptions = new ServiceBusSessionProcessorOptions
                {
                    MaxConcurrentSessions = _options.MaxConcurrentCalls,
                    AutoCompleteMessages = false,
                    MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(_options.MaxAutoLockRenewalMinutes)
                };
                _sessionProcessor = _client.CreateSessionProcessor(_options.TopicName, _options.SubscriptionName, sessionOptions);
            }
            else
            {
                _processor = _client.CreateProcessor(_options.TopicName, _options.SubscriptionName, processorOptions);
            }
        }
        else
        {
            throw new InvalidOperationException(
                "Either QueueName or both TopicName and SubscriptionName must be configured");
        }

        if (_processor != null)
        {
            _processor.ProcessMessageAsync += ProcessMessageHandlerAsync;
            _processor.ProcessErrorAsync += ProcessErrorHandlerAsync;
        }
        else if (_sessionProcessor != null)
        {
            _sessionProcessor.ProcessMessageAsync += ProcessSessionMessageHandlerAsync;
            _sessionProcessor.ProcessErrorAsync += ProcessErrorHandlerAsync;
        }

        _logger.LogInformation("Service Bus Subscriber initialized");
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
    /// Starts processing messages from Service Bus.
    /// </summary>
    public async Task StartProcessingAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Service Bus Subscriber");
        if (_processor != null)
        {
            await _processor.StartProcessingAsync(cancellationToken);
        }
        else if (_sessionProcessor != null)
        {
            await _sessionProcessor.StartProcessingAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Stops processing messages from Service Bus.
    /// </summary>
    public async Task StopProcessingAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping Service Bus Subscriber");
        if (_processor != null)
        {
            await _processor.StopProcessingAsync(cancellationToken);
        }
        else if (_sessionProcessor != null)
        {
            await _sessionProcessor.StopProcessingAsync(cancellationToken);
        }
    }

    private async Task ProcessMessageHandlerAsync(ProcessMessageEventArgs args)
    {
        try
        {
            var eventTypeName = args.Message.ApplicationProperties.TryGetValue("EventType", out var type)
                ? type.ToString()
                : null;

            if (string.IsNullOrEmpty(eventTypeName) || !_eventTypeRegistry.TryGetValue(eventTypeName, out var clrType))
            {
                _logger.LogWarning(
                    "No CLR type registered for event type {EventType}. Dead lettering message.",
                    eventTypeName);

                if (_options.EnableDeadLetter)
                {
                    await args.DeadLetterMessageAsync(
                        args.Message,
                        "Unknown event type",
                        $"No handler registered for {eventTypeName}",
                        args.CancellationToken);
                }
                else
                {
                    await args.CompleteMessageAsync(args.Message, args.CancellationToken);
                }
                return;
            }

            var json = Encoding.UTF8.GetString(args.Message.Body);
            var domainEvent = JsonSerializer.Deserialize(json, clrType, JsonDefaults.Options) as IDomainEvent;

            if (domainEvent == null)
            {
                _logger.LogWarning("Failed to deserialize message {MessageId} to type {EventType}",
                    args.Message.MessageId,
                    eventTypeName);

                if (_options.EnableDeadLetter)
                {
                    await args.DeadLetterMessageAsync(
                        args.Message,
                        "Deserialization failed",
                        $"Could not deserialize to {clrType.Name}",
                        args.CancellationToken);
                }
                else
                {
                    await args.CompleteMessageAsync(args.Message, args.CancellationToken);
                }
                return;
            }

            if (!_handlers.TryGetValue(clrType, out var handler))
            {
                _logger.LogWarning("No handler registered for event type {EventType}", clrType.Name);

                if (_options.EnableDeadLetter)
                {
                    await args.DeadLetterMessageAsync(
                        args.Message,
                        "No handler",
                        $"No handler registered for {clrType.Name}",
                        args.CancellationToken);
                }
                else
                {
                    await args.CompleteMessageAsync(args.Message, args.CancellationToken);
                }
                return;
            }

            _logger.LogInformation(
                "Processing message {MessageId} of type {EventType}",
                args.Message.MessageId,
                clrType.Name);

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

            _logger.LogInformation("Successfully processed message {MessageId}", args.Message.MessageId);

            // Complete the message
            await args.CompleteMessageAsync(args.Message, args.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing message {MessageId}",
                args.Message.MessageId);

            // Abandon message so it can be retried
            await args.AbandonMessageAsync(args.Message, cancellationToken: args.CancellationToken);
        }
    }

    private async Task ProcessSessionMessageHandlerAsync(ProcessSessionMessageEventArgs args)
    {
        try
        {
            var eventTypeName = args.Message.ApplicationProperties.TryGetValue("EventType", out var type)
                ? type.ToString()
                : null;

            if (string.IsNullOrEmpty(eventTypeName) || !_eventTypeRegistry.TryGetValue(eventTypeName, out var clrType))
            {
                _logger.LogWarning(
                    "No CLR type registered for event type {EventType} in session {SessionId}. Dead lettering message.",
                    eventTypeName,
                    args.SessionId);

                if (_options.EnableDeadLetter)
                {
                    await args.DeadLetterMessageAsync(
                        args.Message,
                        "Unknown event type",
                        $"No handler registered for {eventTypeName}",
                        args.CancellationToken);
                }
                else
                {
                    await args.CompleteMessageAsync(args.Message, args.CancellationToken);
                }
                return;
            }

            var json = Encoding.UTF8.GetString(args.Message.Body);
            var domainEvent = JsonSerializer.Deserialize(json, clrType, JsonDefaults.Options) as IDomainEvent;

            if (domainEvent == null)
            {
                _logger.LogWarning(
                    "Failed to deserialize message {MessageId} in session {SessionId} to type {EventType}",
                    args.Message.MessageId,
                    args.SessionId,
                    eventTypeName);

                if (_options.EnableDeadLetter)
                {
                    await args.DeadLetterMessageAsync(
                        args.Message,
                        "Deserialization failed",
                        $"Could not deserialize to {clrType.Name}",
                        args.CancellationToken);
                }
                else
                {
                    await args.CompleteMessageAsync(args.Message, args.CancellationToken);
                }
                return;
            }

            if (!_handlers.TryGetValue(clrType, out var handler))
            {
                _logger.LogWarning(
                    "No handler registered for event type {EventType} in session {SessionId}",
                    clrType.Name,
                    args.SessionId);

                if (_options.EnableDeadLetter)
                {
                    await args.DeadLetterMessageAsync(
                        args.Message,
                        "No handler",
                        $"No handler registered for {clrType.Name}",
                        args.CancellationToken);
                }
                else
                {
                    await args.CompleteMessageAsync(args.Message, args.CancellationToken);
                }
                return;
            }

            _logger.LogInformation(
                "Processing message {MessageId} of type {EventType} in session {SessionId}",
                args.Message.MessageId,
                clrType.Name,
                args.SessionId);

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

            _logger.LogInformation(
                "Successfully processed message {MessageId} in session {SessionId}",
                args.Message.MessageId,
                args.SessionId);

            // Complete the message
            await args.CompleteMessageAsync(args.Message, args.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing message {MessageId} in session {SessionId}",
                args.Message.MessageId,
                args.SessionId);

            // Abandon message so it can be retried
            await args.AbandonMessageAsync(args.Message, cancellationToken: args.CancellationToken);
        }
    }

    private Task ProcessErrorHandlerAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(
            args.Exception,
            "Error in Service Bus processor: {ErrorSource}",
            args.ErrorSource);

        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_processor != null)
        {
            await _processor.StopProcessingAsync();
            await _processor.DisposeAsync();
        }
        else if (_sessionProcessor != null)
        {
            await _sessionProcessor.StopProcessingAsync();
            await _sessionProcessor.DisposeAsync();
        }
        
        await _client.DisposeAsync();
        _logger.LogInformation("Service Bus Subscriber disposed");
    }
}
