using Agents.Domain.Notification.Entities;
using Agents.Infrastructure.Persistence.SqlServer.Data;
using Microsoft.EntityFrameworkCore;

namespace Agents.Infrastructure.Persistence.SqlServer.Repositories;

/// <summary>
/// Notification repository implementation with extended query methods.
/// </summary>
public class NotificationRepository : SqlServerRepository<Notification, string>, INotificationRepository
{
    private readonly AgentsDbContext _context;

    public NotificationRepository(AgentsDbContext context) : base(context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Notification>> GetByStatusAsync(NotificationStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .Where(n => n.Status == status)
            .OrderByDescending(n => n.SentAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Notification>> GetByChannelAsync(string channel, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .Where(n => n.Channel == channel)
            .OrderByDescending(n => n.SentAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Notification>> GetRecentAsync(int days = 7, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTimeOffset.UtcNow.AddDays(-days);

        return await _context.Notifications
            .Where(n => n.SentAt.HasValue && n.SentAt >= cutoffDate)
            .OrderByDescending(n => n.SentAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Notification>> GetRetryableAsync(int maxRetries = 3, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .Where(n => n.Status == NotificationStatus.Failed && n.RetryCount < maxRetries)
            .OrderBy(n => n.SentAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Notification>> GetByRecipientAsync(string recipient, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .Where(n => n.Recipient == recipient)
            .OrderByDescending(n => n.SentAt)
            .ToListAsync(cancellationToken);
    }
}
