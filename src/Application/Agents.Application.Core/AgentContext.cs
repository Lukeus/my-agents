namespace Agents.Application.Core;

/// <summary>
/// Execution context for agents containing correlation IDs, metadata, and execution state
/// </summary>
public class AgentContext
{
    /// <summary>
    /// Unique identifier for this agent execution
    /// </summary>
    public string ExecutionId { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Correlation ID for tracing across multiple agents/services
    /// </summary>
    public string CorrelationId { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Timestamp when execution started
    /// </summary>
    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Additional metadata for the execution
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();

    /// <summary>
    /// User or system that initiated the execution
    /// </summary>
    public string? InitiatedBy { get; init; }

    /// <summary>
    /// Cancellation token for the execution
    /// </summary>
    public CancellationToken CancellationToken { get; init; } = default;
}
