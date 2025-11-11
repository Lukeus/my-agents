using Microsoft.SemanticKernel;

namespace Agents.Application.Core;

/// <summary>
/// Abstraction for LLM provider (Azure OpenAI, Ollama, etc.)
/// </summary>
public interface ILLMProvider
{
    /// <summary>
    /// Gets the Semantic Kernel instance configured for this provider
    /// </summary>
    Kernel GetKernel();

    /// <summary>
    /// Gets the provider name (e.g., "AzureOpenAI", "Ollama")
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Gets whether the provider is available and configured
    /// </summary>
    bool IsAvailable { get; }
}
