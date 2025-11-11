using Agents.Application.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Agents.Infrastructure.LLM;

/// <summary>
/// Dependency injection extensions for LLM infrastructure
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds LLM provider services to the DI container
    /// </summary>
    public static IServiceCollection AddLLMProvider(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind configuration
        services.Configure<LLMProviderOptions>(
            configuration.GetSection(LLMProviderOptions.SectionName).Bind);

        // Register factory and provider
        services.AddSingleton<LLMProviderFactory>();
        services.AddSingleton<ILLMProvider>(sp =>
        {
            var factory = sp.GetRequiredService<LLMProviderFactory>();
            return factory.CreateProvider();
        });

        return services;
    }
}
