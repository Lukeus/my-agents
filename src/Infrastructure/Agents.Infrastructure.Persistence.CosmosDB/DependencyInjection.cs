using Agents.Infrastructure.Persistence.CosmosDB.Configuration;
using Agents.Infrastructure.Persistence.CosmosDB.EventStore;
using Agents.Infrastructure.Persistence.CosmosDB.Initialization;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Agents.Infrastructure.Persistence.CosmosDB;

/// <summary>
/// Dependency injection extensions for Cosmos DB persistence.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds Cosmos DB persistence services to the service collection.
    /// </summary>
    public static IServiceCollection AddCosmosDbPersistence(
        this IServiceCollection services,
        Action<CosmosDbOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        // Configure options
        services.Configure(configureOptions);

        // Register CosmosClient as singleton
        services.AddSingleton<CosmosClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<CosmosDbOptions>>().Value;
            return new CosmosClient(options.ConnectionString, new CosmosClientOptions
            {
                SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                },
                ConnectionMode = ConnectionMode.Direct,
                MaxRetryAttemptsOnRateLimitedRequests = 5,
                MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(10)
            });
        });

        // Register initializer
        services.AddSingleton<CosmosDbInitializer>();

        // Register event store
        services.AddSingleton<IEventStore>(sp =>
        {
            var client = sp.GetRequiredService<CosmosClient>();
            var options = sp.GetRequiredService<IOptions<CosmosDbOptions>>().Value;
            var container = client.GetContainer(options.DatabaseName, options.Containers.Events);
            return new EventStore.EventStore(container);
        });

        // Register container factory for repositories
        services.AddSingleton<ICosmosContainerFactory, CosmosContainerFactory>();

        return services;
    }
}

/// <summary>
/// Factory for creating Cosmos DB containers.
/// </summary>
public interface ICosmosContainerFactory
{
    /// <summary>
    /// Gets a container by name.
    /// </summary>
    Container GetContainer(string containerName);
}

/// <summary>
/// Implementation of container factory.
/// </summary>
internal class CosmosContainerFactory : ICosmosContainerFactory
{
    private readonly CosmosClient _cosmosClient;
    private readonly CosmosDbOptions _options;

    public CosmosContainerFactory(CosmosClient cosmosClient, IOptions<CosmosDbOptions> options)
    {
        _cosmosClient = cosmosClient ?? throw new ArgumentNullException(nameof(cosmosClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public Container GetContainer(string containerName)
    {
        return _cosmosClient.GetContainer(_options.DatabaseName, containerName);
    }
}
