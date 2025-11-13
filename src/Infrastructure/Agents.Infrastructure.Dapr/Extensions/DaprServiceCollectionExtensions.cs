using Agents.Domain.Core.Interfaces;
using Agents.Infrastructure.Dapr.PubSub;
using Agents.Infrastructure.Dapr.State;
using Dapr.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Agents.Infrastructure.Dapr.Extensions;

/// <summary>
/// Extension methods for registering Dapr services in the DI container
/// </summary>
public static class DaprServiceCollectionExtensions
{
    /// <summary>
    /// Adds Dapr client to the service collection
    /// </summary>
    public static IServiceCollection AddDaprInfrastructure(this IServiceCollection services)
    {
        // Register Dapr client
        services.AddDaprClient();

        // Register Dapr-based implementations
        services.AddSingleton<IEventPublisher, DaprEventPublisher>();
        services.AddSingleton<IDaprStateStore, DaprStateStore>();

        return services;
    }

    /// <summary>
    /// Adds Dapr event publisher to the service collection
    /// </summary>
    public static IServiceCollection AddDaprEventPublisher(this IServiceCollection services)
    {
        services.AddSingleton<IEventPublisher, DaprEventPublisher>();
        return services;
    }

    /// <summary>
    /// Adds Dapr state store to the service collection
    /// </summary>
    public static IServiceCollection AddDaprStateStore(this IServiceCollection services)
    {
        services.AddSingleton<IDaprStateStore, DaprStateStore>();
        return services;
    }

    /// <summary>
    /// Adds all Dapr services with custom Dapr client configuration
    /// </summary>
    public static IServiceCollection AddDaprInfrastructure(
        this IServiceCollection services,
        Action<DaprClientBuilder> configureDapr)
    {
        if (configureDapr == null)
        {
            throw new ArgumentNullException(nameof(configureDapr));
        }

        // Register Dapr client with custom configuration
        var daprClientBuilder = new DaprClientBuilder();
        configureDapr(daprClientBuilder);
        services.AddSingleton(daprClientBuilder.Build());

        // Register Dapr-based implementations
        services.AddSingleton<IEventPublisher, DaprEventPublisher>();
        services.AddSingleton<IDaprStateStore, DaprStateStore>();

        return services;
    }
}
