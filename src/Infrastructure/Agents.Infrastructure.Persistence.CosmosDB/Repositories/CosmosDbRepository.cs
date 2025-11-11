using Agents.Domain.Core.Entities;
using Agents.Domain.Core.Interfaces;
using Microsoft.Azure.Cosmos;
using System.Net;

namespace Agents.Infrastructure.Persistence.CosmosDB.Repositories;

/// <summary>
/// Base repository implementation for Cosmos DB.
/// </summary>
/// <typeparam name="TAggregate">The aggregate root type.</typeparam>
/// <typeparam name="TId">The identifier type.</typeparam>
public class CosmosDbRepository<TAggregate, TId> : IRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TId>
    where TId : notnull
{
    private readonly Container _container;

    public CosmosDbRepository(Container container)
    {
        _container = container ?? throw new ArgumentNullException(nameof(container));
    }

    /// <inheritdoc />
    public async Task<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _container.ReadItemAsync<TAggregate>(
                id.ToString()!,
                new PartitionKey(id.ToString()),
                cancellationToken: cancellationToken);

            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        await _container.CreateItemAsync(
            aggregate,
            new PartitionKey(aggregate.Id.ToString()),
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        await _container.ReplaceItemAsync(
            aggregate,
            aggregate.Id.ToString()!,
            new PartitionKey(aggregate.Id.ToString()),
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(TId id, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id);

        await _container.DeleteItemAsync<TAggregate>(
            id.ToString()!,
            new PartitionKey(id.ToString()),
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Cosmos DB operations are immediately consistent within a partition
        // No explicit save needed
        return Task.FromResult(0);
    }
}
