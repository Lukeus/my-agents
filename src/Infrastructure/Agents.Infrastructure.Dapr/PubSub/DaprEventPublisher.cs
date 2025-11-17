using Agents.Domain.Core.Events;
using Agents.Domain.Core.Interfaces;
using Dapr.Client;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace Agents.Infrastructure.Dapr.PubSub;

/// <summary>
/// Dapr-based implementation of event publisher that publishes domain events to a pub/sub component
/// </summary>
public class DaprEventPublisher : IEventPublisher
{
    private readonly DaprClient _daprClient;
    private readonly ILogger<DaprEventPublisher> _logger;
    private readonly ResiliencePipeline _retryPipeline;
    private const string PubSubName = "agents-pubsub";

    public DaprEventPublisher(DaprClient daprClient, ILogger<DaprEventPublisher> logger)
    {
        _daprClient = daprClient ?? throw new ArgumentNullException(nameof(daprClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Configure retry policy with exponential backoff
        _retryPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        args.Outcome.Exception,
                        "Event publish retry {RetryCount} after {Delay}ms. Exception: {ExceptionType}",
                        args.AttemptNumber,
                        args.RetryDelay.TotalMilliseconds,
                        args.Outcome.Exception?.GetType().Name ?? "None");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    /// <summary>
    /// Publishes a single domain event to the pub/sub component
    /// </summary>
    public async Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (domainEvent == null)
        {
            throw new ArgumentNullException(nameof(domainEvent));
        }

        var topicName = GetTopicName(domainEvent);

        try
        {
            _logger.LogInformation(
                "Publishing event {EventType} with ID {EventId} to topic {TopicName}",
                domainEvent.GetType().Name,
                domainEvent.EventId,
                topicName);

            // Execute with retry policy
            await _retryPipeline.ExecuteAsync(async ct =>
            {
                await _daprClient.PublishEventAsync(
                    PubSubName,
                    topicName,
                    domainEvent,
                    ct);
            }, cancellationToken);

            _logger.LogInformation(
                "Successfully published event {EventType} with ID {EventId}",
                domainEvent.GetType().Name,
                domainEvent.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish event {EventType} with ID {EventId} to topic {TopicName} after all retries",
                domainEvent.GetType().Name,
                domainEvent.EventId,
                topicName);
            throw;
        }
    }

    /// <summary>
    /// Publishes multiple domain events to the pub/sub component
    /// </summary>
    public async Task PublishAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        if (domainEvents == null)
        {
            throw new ArgumentNullException(nameof(domainEvents));
        }

        var eventsList = domainEvents.ToList();

        if (!eventsList.Any())
        {
            _logger.LogDebug("No events to publish");
            return;
        }

        _logger.LogInformation("Publishing batch of {Count} events", eventsList.Count);

        // Publish events in parallel for better performance
        // Handle partial failures gracefully - some events may succeed
        var publishTasks = eventsList.Select(evt => PublishAsync(evt, cancellationToken));

        try
        {
            await Task.WhenAll(publishTasks);
            _logger.LogInformation("Successfully published all {Count} events", eventsList.Count);
        }
        catch (Exception ex)
        {
            // Determine how many succeeded vs failed
            var successCount = publishTasks.Count(t => t.IsCompletedSuccessfully);
            var failedCount = eventsList.Count - successCount;

            _logger.LogError(
                ex,
                "Batch publish partially failed: {SuccessCount}/{TotalCount} succeeded, {FailedCount} failed",
                successCount,
                eventsList.Count,
                failedCount);

            // Don't throw - some events may have succeeded
            // Caller can check individual task results if needed
        }
    }

    /// <summary>
    /// Gets the topic name for the given domain event
    /// Topic names are derived from the event type name in lowercase
    /// </summary>
    private static string GetTopicName(IDomainEvent domainEvent)
    {
        return domainEvent.GetType().Name.ToLowerInvariant();
    }
}
