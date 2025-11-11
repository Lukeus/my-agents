namespace Agents.Infrastructure.ServiceBus.Configuration;

/// <summary>
/// Configuration options for Azure Service Bus.
/// </summary>
public class ServiceBusOptions
{
    public const string SectionName = "ServiceBus";

    /// <summary>
    /// Service Bus namespace connection string.
    /// </summary>
    public required string ConnectionString { get; set; }

    /// <summary>
    /// Default queue name for messages.
    /// </summary>
    public string? QueueName { get; set; }

    /// <summary>
    /// Default topic name for publish/subscribe.
    /// </summary>
    public string? TopicName { get; set; }

    /// <summary>
    /// Subscription name for receiving from topics.
    /// </summary>
    public string? SubscriptionName { get; set; }

    /// <summary>
    /// Maximum number of concurrent calls for message processing.
    /// </summary>
    public int MaxConcurrentCalls { get; set; } = 10;

    /// <summary>
    /// Maximum auto lock renewal duration in minutes.
    /// </summary>
    public int MaxAutoLockRenewalMinutes { get; set; } = 5;

    /// <summary>
    /// Maximum retry attempts for failed operations.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Base delay between retries in milliseconds.
    /// </summary>
    public int RetryDelayMilliseconds { get; set; } = 1000;

    /// <summary>
    /// Use exponential backoff for retries.
    /// </summary>
    public bool UseExponentialBackoff { get; set; } = true;

    /// <summary>
    /// Enable dead letter queue for failed messages.
    /// </summary>
    public bool EnableDeadLetter { get; set; } = true;

    /// <summary>
    /// Enable session support for ordered message processing.
    /// </summary>
    public bool EnableSessions { get; set; } = false;
}
