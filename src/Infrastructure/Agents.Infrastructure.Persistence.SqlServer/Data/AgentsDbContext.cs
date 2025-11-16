using Agents.Domain.BimClassification.Entities;
using Agents.Domain.Notification.Entities;
using Microsoft.EntityFrameworkCore;

namespace Agents.Infrastructure.Persistence.SqlServer.Data;

/// <summary>
/// Entity Framework DbContext for agents.
/// </summary>
public class AgentsDbContext : DbContext
{
    public AgentsDbContext(DbContextOptions<AgentsDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the Notifications DbSet.
    /// </summary>
    public DbSet<Notification> Notifications => Set<Notification>();

    /// <summary>
    /// Gets or sets the BIM Classification Suggestions DbSet.
    /// </summary>
    public DbSet<BimClassificationSuggestion> BimClassificationSuggestions => Set<BimClassificationSuggestion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AgentsDbContext).Assembly);
    }
}
