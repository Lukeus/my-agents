using Agents.Application.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace Agents.Infrastructure.LLM;

/// <summary>
/// Ollama provider for local LLM development
/// </summary>
public class OllamaProvider : ILLMProvider
{
    private readonly LLMProviderOptions _options;
    private readonly ILogger<OllamaProvider> _logger;
    private Kernel? _kernel;

    public string ProviderName => "Ollama";

    public bool IsAvailable
    {
        get
        {
            return _options.Ollama != null &&
                   !string.IsNullOrWhiteSpace(_options.Ollama.Endpoint) &&
                   !string.IsNullOrWhiteSpace(_options.Ollama.ModelId);
        }
    }

    public OllamaProvider(IOptions<LLMProviderOptions> options, ILogger<OllamaProvider> logger)
    {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Kernel GetKernel()
    {
        if (_kernel != null)
        {
            return _kernel;
        }

        if (!IsAvailable || _options.Ollama == null)
        {
            throw new InvalidOperationException(
                "Ollama provider is not properly configured. Check LLMProvider:Ollama settings.");
        }

        _logger.LogInformation(
            "Initializing Ollama provider with endpoint: {Endpoint}, model: {Model}",
            _options.Ollama.Endpoint,
            _options.Ollama.ModelId);

        var builder = Kernel.CreateBuilder();

        builder.AddOllamaChatCompletion(
            modelId: _options.Ollama.ModelId,
            endpoint: new Uri(_options.Ollama.Endpoint));

        _kernel = builder.Build();

        _logger.LogInformation("Ollama provider initialized successfully");

        return _kernel;
    }
}
