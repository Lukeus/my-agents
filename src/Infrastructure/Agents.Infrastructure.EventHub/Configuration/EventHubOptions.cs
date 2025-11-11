namespace Agents.Infrastructure.EventHub.Configuration;

/// <summary>
/// Configuration options for Azure Event Hubs.
/// </summary>
public class EventHubOptions
{
    public const string SectionName = "EventHub";

    /// <summary>
    /// Event Hub namespace connection string.
    /// </summary>
    public required string ConnectionString { get; set; }

    /// <summary>
    /// Name of the Event Hub.
    /// </summary>
    public required string EventHubName { get; set; }

    /// <summary>
    /// Consumer group name for receiving events.
    /// </summary>
    public string ConsumerGroup { get; set; } = "$Default";

    /// <summary>
    /// Blob storage connection string for checkpointing.
    /// </summary>
    public string? BlobStorageConnectionString { get; set; }

    /// <summary>
    /// Blob container name for storing checkpoints.
    /// </summary>
    public string CheckpointContainerName { get; set; } = "checkpoints";

    /// <summary>
    /// Maximum batch size for batch operations.
    /// </summary>
    public int MaxBatchSize { get; set; } = 100;

    /// <summary>
    /// Maximum wait time in seconds before sending a batch.
    /// </summary>
    public int MaxWaitTimeSeconds { get; set; } = 30;

    /// <summary>
    /// Enable automatic checkpointing after processing events.
    /// </summary>
    public bool EnableAutoCheckpoint { get; set; } = true;
}
