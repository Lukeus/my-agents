using Agents.Domain.Core.Entities;
using Agents.Domain.Notification.Events;

namespace Agents.Domain.Notification.Entities;

/// <summary>
/// Notification aggregate root representing a notification sent through a delivery channel.
/// </summary>
public class Notification : AggregateRoot<string>
{
    /// <summary>
    /// Gets the notification delivery channel (e.g., "email", "sms", "teams", "slack").
    /// </summary>
    public string Channel { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the recipient identifier (email address, phone number, user ID, etc.).
    /// </summary>
    public string Recipient { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the notification subject or title.
    /// </summary>
    public string Subject { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the raw notification content.
    /// </summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the formatted content after applying channel-specific formatting.
    /// </summary>
    public string? FormattedContent { get; private set; }

    /// <summary>
    /// Gets the current status of the notification.
    /// </summary>
    public NotificationStatus Status { get; private set; }

    /// <summary>
    /// Gets the timestamp when the notification was sent.
    /// </summary>
    public DateTimeOffset? SentAt { get; private set; }

    /// <summary>
    /// Gets the timestamp when the notification was confirmed as delivered.
    /// </summary>
    public DateTimeOffset? DeliveredAt { get; private set; }

    /// <summary>
    /// Gets the number of times delivery has been retried.
    /// </summary>
    public int RetryCount { get; private set; }

    /// <summary>
    /// Gets the error message if the notification failed.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    private Notification() : base(string.Empty) { } // EF Core

    /// <summary>
    /// Creates a new notification instance.
    /// </summary>
    /// <param name="id">The unique identifier for the notification.</param>
    /// <param name="channel">The delivery channel.</param>
    /// <param name="recipient">The recipient identifier.</param>
    /// <param name="subject">The notification subject.</param>
    /// <param name="content">The notification content.</param>
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

    /// <summary>
    /// Marks the notification as formatted with channel-specific formatting applied.
    /// </summary>
    /// <param name="formattedContent">The formatted content.</param>
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

    /// <summary>
    /// Marks the notification as successfully sent.
    /// </summary>
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

    /// <summary>
    /// Marks the notification as successfully delivered.
    /// </summary>
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

    /// <summary>
    /// Marks the notification as failed with an error message and increments retry count.
    /// </summary>
    /// <param name="errorMessage">The error message describing the failure.</param>
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

    /// <summary>
    /// Determines whether the notification can be retried.
    /// </summary>
    /// <param name="maxRetries">The maximum number of retry attempts allowed.</param>
    /// <returns>True if the notification can be retried; otherwise, false.</returns>
    public bool CanRetry(int maxRetries = 3)
    {
        return RetryCount < maxRetries && Status == NotificationStatus.Failed;
    }
}

/// <summary>
/// Represents the status of a notification in its lifecycle.
/// </summary>
public enum NotificationStatus
{
    /// <summary>
    /// Notification has been created but not yet processed.
    /// </summary>
    Pending,

    /// <summary>
    /// Notification content has been formatted for the target channel.
    /// </summary>
    Formatted,

    /// <summary>
    /// Notification has been sent to the delivery channel.
    /// </summary>
    Sent,

    /// <summary>
    /// Notification has been confirmed as delivered to the recipient.
    /// </summary>
    Delivered,

    /// <summary>
    /// Notification failed to send or deliver.
    /// </summary>
    Failed
}
