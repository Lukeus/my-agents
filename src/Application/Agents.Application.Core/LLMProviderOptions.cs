namespace Agents.Application.Core;

/// <summary>
/// Configuration options for LLM providers
/// </summary>
public class LLMProviderOptions
{
    public const string SectionName = "LLMProvider";

    /// <summary>
    /// Provider type: "AzureOpenAI" or "Ollama"
    /// </summary>
    public string ProviderType { get; set; } = "Ollama";

    /// <summary>
    /// Azure OpenAI specific settings
    /// </summary>
    public AzureOpenAISettings? AzureOpenAI { get; set; }

    /// <summary>
    /// Ollama specific settings
    /// </summary>
    public OllamaSettings? Ollama { get; set; }
}

/// <summary>
/// Azure OpenAI specific configuration settings.
/// </summary>
public class AzureOpenAISettings
{
    /// <summary>
    /// Gets or sets the Azure OpenAI endpoint URL.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the API key for authentication.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the deployment name for the Azure OpenAI resource.
    /// </summary>
    public string DeploymentName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model identifier (e.g., "gpt-4", "gpt-3.5-turbo").
    /// </summary>
    public string ModelId { get; set; } = "gpt-4";
}

/// <summary>
/// Ollama local LLM configuration settings.
/// </summary>
public class OllamaSettings
{
    /// <summary>
    /// Gets or sets the Ollama endpoint URL.
    /// </summary>
    public string Endpoint { get; set; } = "http://localhost:11434";

    /// <summary>
    /// Gets or sets the model identifier (e.g., "llama3.2", "mistral").
    /// </summary>
    public string ModelId { get; set; } = "llama3.2";
}
