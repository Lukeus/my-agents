using Agents.Domain.Core.Interfaces;
using Agents.Domain.Notification.Entities;
using Agents.Infrastructure.Persistence.SqlServer.Data;
using Agents.Infrastructure.Persistence.SqlServer.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Agents.Infrastructure.Persistence.SqlServer;

/// <summary>
/// Dependency injection extensions for SQL Server persistence.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds SQL Server persistence services to the service collection.
    /// </summary>
    public static IServiceCollection AddSqlServerPersistence(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        // Register DbContext
        services.AddDbContext<AgentsDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null);
                sqlOptions.CommandTimeout(30);
            });
        });

        // Register repositories
        services.AddScoped<IRepository<Notification, string>>(sp =>
        {
            var context = sp.GetRequiredService<AgentsDbContext>();
            return new SqlServerRepository<Notification, string>(context);
        });

        return services;
    }

    /// <summary>
    /// Applies database migrations.
    /// </summary>
    public static async Task MigrateDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AgentsDbContext>();
        await context.Database.MigrateAsync();
    }
}
