using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Agents.Infrastructure.Persistence.SqlServer.Data;

/// <summary>
/// Design-time factory for creating AgentsDbContext instances for EF migrations.
/// </summary>
public class AgentsDbContextFactory : IDesignTimeDbContextFactory<AgentsDbContext>
{
    public AgentsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AgentsDbContext>();
        
        // Use a temporary connection string for migrations
        // This is only used at design time, not runtime
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=AgentsDb;Trusted_Connection=True;");
        
        return new AgentsDbContext(optionsBuilder.Options);
    }
}
