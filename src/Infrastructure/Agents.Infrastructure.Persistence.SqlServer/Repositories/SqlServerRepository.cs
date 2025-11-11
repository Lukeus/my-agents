using Agents.Domain.Core.Entities;
using Agents.Domain.Core.Interfaces;
using Agents.Infrastructure.Persistence.SqlServer.Data;
using Microsoft.EntityFrameworkCore;

namespace Agents.Infrastructure.Persistence.SqlServer.Repositories;

/// <summary>
/// SQL Server repository implementation using Entity Framework Core.
/// </summary>
/// <typeparam name="TAggregate">The aggregate root type.</typeparam>
/// <typeparam name="TId">The identifier type.</typeparam>
public class SqlServerRepository<TAggregate, TId> : IRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TId>
    where TId : notnull
{
    private readonly AgentsDbContext _context;
    private readonly DbSet<TAggregate> _dbSet;

    public SqlServerRepository(AgentsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = context.Set<TAggregate>();
    }

    /// <inheritdoc />
    public async Task<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        await _dbSet.AddAsync(aggregate, cancellationToken);
    }

    /// <inheritdoc />
    public Task UpdateAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        _dbSet.Update(aggregate);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteAsync(TId id, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id);
        
        var entity = _dbSet.Local.FirstOrDefault(e => EqualityComparer<TId>.Default.Equals(e.Id, id));
        
        if (entity != null)
        {
            _dbSet.Remove(entity);
        }
        else
        {
            // Create a stub entity for deletion
            entity = Activator.CreateInstance<TAggregate>();
            var idProperty = typeof(TAggregate).GetProperty(nameof(AggregateRoot<TId>.Id));
            idProperty?.SetValue(entity, id);
            _context.Entry(entity).State = EntityState.Deleted;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
