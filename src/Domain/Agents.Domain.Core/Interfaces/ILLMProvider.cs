namespace Agents.Domain.Core.Interfaces;

/// <summary>
/// Interface for LLM providers (Ollama, Azure OpenAI, etc.).
/// Provides abstraction over different LLM implementations.
/// </summary>
public interface ILLMProvider
{
    /// <summary>
    /// Completes a prompt and returns the response as a string.
    /// </summary>
    /// <param name="prompt">The prompt to send to the LLM.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The LLM's response.</returns>
    Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes a prompt and deserializes the response to the specified type.
    /// </summary>
    /// <typeparam name="TResult">The expected result type.</typeparam>
    /// <param name="prompt">The prompt to send to the LLM.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The deserialized response.</returns>
    Task<TResult> CompleteAsync<TResult>(string prompt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams the completion response token by token.
    /// </summary>
    /// <param name="prompt">The prompt to send to the LLM.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of response tokens.</returns>
    IAsyncEnumerable<string> StreamCompleteAsync(string prompt, CancellationToken cancellationToken = default);
}
