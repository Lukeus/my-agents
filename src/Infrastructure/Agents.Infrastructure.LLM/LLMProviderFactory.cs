using Agents.Application.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Agents.Infrastructure.LLM;

/// <summary>
/// Factory to create the appropriate LLM provider based on configuration
/// </summary>
public class LLMProviderFactory
{
    private readonly IOptions<LLMProviderOptions> _options;
    private readonly ILoggerFactory _loggerFactory;

    public LLMProviderFactory(
        IOptions<LLMProviderOptions> options,
        ILoggerFactory loggerFactory)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <summary>
    /// Creates the configured LLM provider
    /// </summary>
    public ILLMProvider CreateProvider()
    {
        var providerType = _options.Value.ProviderType;

        return providerType.ToLowerInvariant() switch
        {
            "azureopenai" => new AzureOpenAIProvider(
                _options,
                _loggerFactory.CreateLogger<AzureOpenAIProvider>()),
            
            "ollama" => new OllamaProvider(
                _options,
                _loggerFactory.CreateLogger<OllamaProvider>()),
            
            _ => throw new InvalidOperationException(
                $"Unknown LLM provider type: {providerType}. Supported values: 'AzureOpenAI', 'Ollama'")
        };
    }
}
