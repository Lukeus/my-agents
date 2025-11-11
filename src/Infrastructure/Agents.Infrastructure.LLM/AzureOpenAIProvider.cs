using Agents.Application.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace Agents.Infrastructure.LLM;

/// <summary>
/// Azure OpenAI provider for production deployment
/// </summary>
public class AzureOpenAIProvider : ILLMProvider
{
    private readonly LLMProviderOptions _options;
    private readonly ILogger<AzureOpenAIProvider> _logger;
    private Kernel? _kernel;

    public string ProviderName => "AzureOpenAI";

    public bool IsAvailable
    {
        get
        {
            return _options.AzureOpenAI != null &&
                   !string.IsNullOrWhiteSpace(_options.AzureOpenAI.Endpoint) &&
                   !string.IsNullOrWhiteSpace(_options.AzureOpenAI.ApiKey) &&
                   !string.IsNullOrWhiteSpace(_options.AzureOpenAI.DeploymentName);
        }
    }

    public AzureOpenAIProvider(IOptions<LLMProviderOptions> options, ILogger<AzureOpenAIProvider> logger)
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

        if (!IsAvailable || _options.AzureOpenAI == null)
        {
            throw new InvalidOperationException(
                "Azure OpenAI provider is not properly configured. Check LLMProvider:AzureOpenAI settings.");
        }

        _logger.LogInformation(
            "Initializing Azure OpenAI provider with endpoint: {Endpoint}, deployment: {Deployment}",
            _options.AzureOpenAI.Endpoint,
            _options.AzureOpenAI.DeploymentName);

        var builder = Kernel.CreateBuilder();
        
        builder.AddAzureOpenAIChatCompletion(
            deploymentName: _options.AzureOpenAI.DeploymentName,
            endpoint: _options.AzureOpenAI.Endpoint,
            apiKey: _options.AzureOpenAI.ApiKey,
            modelId: _options.AzureOpenAI.ModelId);

        _kernel = builder.Build();

        _logger.LogInformation("Azure OpenAI provider initialized successfully");

        return _kernel;
    }
}
