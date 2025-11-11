using Agents.Infrastructure.Persistence.CosmosDB.Configuration;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace Agents.Infrastructure.Persistence.CosmosDB.Initialization;

/// <summary>
/// Initializes Cosmos DB database and containers.
/// </summary>
public class CosmosDbInitializer
{
    private readonly CosmosClient _cosmosClient;
    private readonly CosmosDbOptions _options;

    public CosmosDbInitializer(CosmosClient cosmosClient, IOptions<CosmosDbOptions> options)
    {
        _cosmosClient = cosmosClient ?? throw new ArgumentNullException(nameof(cosmosClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Initializes the database and all containers.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Create database
        var database = await _cosmosClient.CreateDatabaseIfNotExistsAsync(
            _options.DatabaseName,
            cancellationToken: cancellationToken);

        // Create containers
        await CreateContainerAsync(database.Database, _options.Containers.Events, "/aggregateId", cancellationToken);
        await CreateContainerAsync(database.Database, _options.Containers.Notifications, "/id", cancellationToken);
        await CreateContainerAsync(database.Database, _options.Containers.ServiceDeskTickets, "/id", cancellationToken);
        await CreateContainerAsync(database.Database, _options.Containers.DevOpsItems, "/id", cancellationToken);
    }

    private static async Task CreateContainerAsync(
        Database database,
        string containerName,
        string partitionKeyPath,
        CancellationToken cancellationToken)
    {
        var containerProperties = new ContainerProperties(containerName, partitionKeyPath)
        {
            // Set default TTL to -1 (no expiration, but can be set per document)
            DefaultTimeToLive = -1
        };

        await database.CreateContainerIfNotExistsAsync(
            containerProperties,
            throughput: 400, // Minimum throughput for development
            cancellationToken: cancellationToken);
    }
}
