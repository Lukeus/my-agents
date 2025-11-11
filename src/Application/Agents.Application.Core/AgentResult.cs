namespace Agents.Application.Core;

/// <summary>
/// Result from an agent execution
/// </summary>
public class AgentResult
{
    public bool IsSuccess { get; init; }
    public string? Output { get; init; }
    public string? ErrorMessage { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
    public TimeSpan Duration { get; init; }

    public static AgentResult Success(string? output = null, Dictionary<string, object>? metadata = null)
    {
        return new AgentResult
        {
            IsSuccess = true,
            Output = output,
            Metadata = metadata ?? new Dictionary<string, object>()
        };
    }

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
/// Typed result from an agent execution
/// </summary>
public class AgentResult<T> : AgentResult
{
    public T? Data { get; init; }

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

    public static AgentResult<T> Failure(string errorMessage, Dictionary<string, object>? metadata = null)
    {
        return new AgentResult<T>
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Metadata = metadata ?? new Dictionary<string, object>()
        };
    }
}
