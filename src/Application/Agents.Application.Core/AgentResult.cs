namespace Agents.Application.Core;

/// <summary>
/// Result from an agent execution.
/// </summary>
public class AgentResult
{
    /// <summary>
    /// Gets a value indicating whether the agent execution was successful.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets the output produced by the agent.
    /// </summary>
    public string? Output { get; init; }

    /// <summary>
    /// Gets the error message if the execution failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets additional metadata about the execution.
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();

    /// <summary>
    /// Gets the duration of the agent execution.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Creates a successful agent result.
    /// </summary>
    /// <param name="output">Optional output from the agent.</param>
    /// <param name="metadata">Optional metadata about the execution.</param>
    /// <returns>A successful agent result.</returns>
    public static AgentResult Success(string? output = null, Dictionary<string, object>? metadata = null)
    {
        return new AgentResult
        {
            IsSuccess = true,
            Output = output,
            Metadata = metadata ?? new Dictionary<string, object>()
        };
    }

    /// <summary>
    /// Creates a failed agent result.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="metadata">Optional metadata about the execution.</param>
    /// <returns>A failed agent result.</returns>
    public static AgentResult Failure(string errorMessage, Dictionary<string, object>? metadata = null)
    {
        return new AgentResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Metadata = metadata ?? new Dictionary<string, object>()
        };
    }
}

/// <summary>
/// Typed result from an agent execution.
/// </summary>
/// <typeparam name="T">The type of data returned by the agent.</typeparam>
public class AgentResult<T> : AgentResult
{
    /// <summary>
    /// Gets the strongly-typed data returned by the agent.
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    /// Creates a successful agent result with data.
    /// </summary>
    /// <param name="data">The data returned by the agent.</param>
    /// <param name="output">Optional output from the agent.</param>
    /// <param name="metadata">Optional metadata about the execution.</param>
    /// <returns>A successful agent result with data.</returns>
    public static AgentResult<T> Success(T data, string? output = null, Dictionary<string, object>? metadata = null)
    {
        return new AgentResult<T>
        {
            IsSuccess = true,
            Data = data,
            Output = output,
            Metadata = metadata ?? new Dictionary<string, object>()
        };
    }

    /// <summary>
    /// Creates a failed agent result.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="metadata">Optional metadata about the execution.</param>
    /// <returns>A failed agent result.</returns>
    public new static AgentResult<T> Failure(string errorMessage, Dictionary<string, object>? metadata = null)
    {
        return new AgentResult<T>
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Metadata = metadata ?? new Dictionary<string, object>()
        };
    }
}
