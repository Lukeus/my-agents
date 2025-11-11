namespace Agents.Infrastructure.Persistence.CosmosDB.Configuration;

/// <summary>
/// Configuration options for Cosmos DB.
/// </summary>
public class CosmosDbOptions
{
    public const string SectionName = "CosmosDB";

    /// <summary>
    /// Gets or sets the Cosmos DB connection string.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the database name.
    /// </summary>
    public string DatabaseName { get; set; } = "AgentsDB";

    /// <summary>
    /// Gets or sets the container configurations.
    /// </summary>
    public CosmosContainerConfiguration Containers { get; set; } = new();
}

/// <summary>
/// Container configuration for Cosmos DB.
/// </summary>
public class CosmosContainerConfiguration
{
    /// <summary>
    /// Gets or sets the events container name.
    /// </summary>
    public string Events { get; set; } = "events";

    /// <summary>
    /// Gets or sets the notifications container name.
    /// </summary>
    public string Notifications { get; set; } = "notifications";

    /// <summary>
    /// Gets or sets the service desk tickets container name.
    /// </summary>
    public string ServiceDeskTickets { get; set; } = "servicedesk-tickets";

    /// <summary>
    /// Gets or sets the DevOps items container name.
    /// </summary>
    public string DevOpsItems { get; set; } = "devops-items";
}
