using Agents.Domain.Core.Entities;

namespace Agents.Domain.Core.Interfaces;

/// <summary>
/// Generic repository interface for aggregate roots.
/// </summary>
/// <typeparam name="TAggregate">The aggregate root type.</typeparam>
/// <typeparam name="TId">The identifier type.</typeparam>
public interface IRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TId>
    where TId : notnull
{
    /// <summary>
    /// Gets an aggregate by its identifier.
    /// </summary>
    Task<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new aggregate to the repository.
    /// </summary>
    Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing aggregate.
    /// </summary>
    Task UpdateAsync(TAggregate aggregate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an aggregate by its identifier.
    /// </summary>
    Task DeleteAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all changes to the repository.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
