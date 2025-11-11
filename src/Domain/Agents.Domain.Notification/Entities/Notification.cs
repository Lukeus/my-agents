using Agents.Domain.Core.Entities;
using Agents.Domain.Notification.Events;

namespace Agents.Domain.Notification.Entities;

/// <summary>
/// Notification aggregate root
/// </summary>
public class Notification : AggregateRoot<string>
{
    public string Channel { get; private set; } = string.Empty;
    public string Recipient { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public string? FormattedContent { get; private set; }
    public NotificationStatus Status { get; private set; }
    public DateTimeOffset? SentAt { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }
    public int RetryCount { get; private set; }
    public string? ErrorMessage { get; private set; }

    private Notification() : base(string.Empty) { } // EF Core

    public Notification(string id, string channel, string recipient, string subject, string content)
        : base(id)
    {
        Channel = channel;
        Recipient = recipient;
        Subject = subject;
        Content = content;
        Status = NotificationStatus.Pending;
        RetryCount = 0;
    }

    public void MarkAsFormatted(string formattedContent)
    {
        FormattedContent = formattedContent;
        
        AddDomainEvent(new NotificationFormattedEvent
        {
            NotificationId = Id,
            Channel = Channel,
            FormattedContent = formattedContent,
            Recipient = Recipient
        });
    }

    public void MarkAsSent()
    {
        Status = NotificationStatus.Sent;
        SentAt = DateTimeOffset.UtcNow;
        
        AddDomainEvent(new NotificationSentEvent
        {
            NotificationId = Id,
            Channel = Channel,
            Recipient = Recipient,
            IsSuccess = true
        });
    }

    public void MarkAsDelivered()
    {
        Status = NotificationStatus.Delivered;
        DeliveredAt = DateTimeOffset.UtcNow;
        
        AddDomainEvent(new NotificationDeliveredEvent
        {
            NotificationId = Id,
            Channel = Channel,
            DeliveredAt = DeliveredAt.Value
        });
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = NotificationStatus.Failed;
        ErrorMessage = errorMessage;
        RetryCount++;
        
        AddDomainEvent(new NotificationFailedEvent
        {
            NotificationId = Id,
            Channel = Channel,
            ErrorMessage = errorMessage,
            RetryCount = RetryCount
        });
    }

    public bool CanRetry(int maxRetries = 3)
    {
        return RetryCount < maxRetries && Status == NotificationStatus.Failed;
    }
}

public enum NotificationStatus
{
    Pending,
    Formatted,
    Sent,
    Delivered,
    Failed
}
