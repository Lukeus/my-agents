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

public class AzureOpenAISettings
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string DeploymentName { get; set; } = string.Empty;
    public string ModelId { get; set; } = "gpt-4";
}

public class OllamaSettings
{
    public string Endpoint { get; set; } = "http://localhost:11434";
    public string ModelId { get; set; } = "llama3.2";
}
