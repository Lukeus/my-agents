using Agents.Domain.Core.Interfaces;
using Agents.Domain.Notification.Entities;

namespace Agents.Infrastructure.Persistence.SqlServer.Repositories;

/// <summary>
/// Extended repository interface for Notification aggregate with query methods.
/// </summary>
public interface INotificationRepository : IRepository<Notification, string>
{
    /// <summary>
    /// Gets notifications by status.
    /// </summary>
    Task<IEnumerable<Notification>> GetByStatusAsync(NotificationStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets notifications by channel.
    /// </summary>
    Task<IEnumerable<Notification>> GetByChannelAsync(string channel, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent notifications (last N days).
    /// </summary>
    Task<IEnumerable<Notification>> GetRecentAsync(int days = 7, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets failed notifications that can be retried.
    /// </summary>
    Task<IEnumerable<Notification>> GetRetryableAsync(int maxRetries = 3, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets notifications by recipient.
    /// </summary>
    Task<IEnumerable<Notification>> GetByRecipientAsync(string recipient, CancellationToken cancellationToken = default);
}
