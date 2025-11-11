namespace Agents.Infrastructure.EventGrid.Configuration;

/// <summary>
/// Configuration options for Azure Event Grid.
/// </summary>
public class EventGridOptions
{
    public const string SectionName = "EventGrid";

    /// <summary>
    /// Event Grid topic endpoint URL.
    /// </summary>
    public required string Endpoint { get; set; }

    /// <summary>
    /// Access key for the Event Grid topic.
    /// </summary>
    public string? AccessKey { get; set; }

    /// <summary>
    /// Use local emulator for development (e.g., Azure Event Grid emulator).
    /// </summary>
    public bool UseEmulator { get; set; } = false;

    /// <summary>
    /// Maximum retry attempts for failed publishes.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Delay between retry attempts in milliseconds.
    /// </summary>
    public int RetryDelayMilliseconds { get; set; } = 1000;

    /// <summary>
    /// Enable dead letter queue for failed events.
    /// </summary>
    public bool EnableDeadLetter { get; set; } = true;

    /// <summary>
    /// Dead letter storage connection string.
    /// </summary>
    public string? DeadLetterStorageConnectionString { get; set; }
}
