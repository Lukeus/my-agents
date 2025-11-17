using Agents.Domain.Core.Events;

namespace Agents.Domain.Notification.Events;

/// <summary>
/// Event raised when a notification is formatted.
/// </summary>
public class NotificationFormattedEvent : DomainEvent
{
    /// <summary>
    /// Gets the notification ID.
    /// </summary>
    public required string NotificationId { get; init; }

    /// <summary>
    /// Gets the notification channel.
    /// </summary>
    public required string Channel { get; init; }

    /// <summary>
    /// Gets the formatted content.
    /// </summary>
    public required string FormattedContent { get; init; }

    /// <summary>
    /// Gets the recipient identifier.
    /// </summary>
    public required string Recipient { get; init; }
}

/// <summary>
/// Event raised when a notification is sent.
/// </summary>
public class NotificationSentEvent : DomainEvent
{
    /// <summary>
    /// Gets the notification ID.
    /// </summary>
    public required string NotificationId { get; init; }

    /// <summary>
    /// Gets the notification channel.
    /// </summary>
    public required string Channel { get; init; }

    /// <summary>
    /// Gets the recipient identifier.
    /// </summary>
    public required string Recipient { get; init; }

    /// <summary>
    /// Gets a value indicating whether the send operation was successful.
    /// </summary>
    public required bool IsSuccess { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Event raised when a notification delivery is confirmed.
/// </summary>
public class NotificationDeliveredEvent : DomainEvent
{
    /// <summary>
    /// Gets the notification ID.
    /// </summary>
    public required string NotificationId { get; init; }

    /// <summary>
    /// Gets the notification channel.
    /// </summary>
    public required string Channel { get; init; }

    /// <summary>
    /// Gets the timestamp when the notification was delivered.
    /// </summary>
    public required DateTimeOffset DeliveredAt { get; init; }
}

/// <summary>
/// Event raised when a notification delivery fails.
/// </summary>
public class NotificationFailedEvent : DomainEvent
{
    /// <summary>
    /// Gets the notification ID.
    /// </summary>
    public required string NotificationId { get; init; }

    /// <summary>
    /// Gets the notification channel.
    /// </summary>
    public required string Channel { get; init; }

    /// <summary>
    /// Gets the error message describing the failure.
    /// </summary>
    public required string ErrorMessage { get; init; }

    /// <summary>
    /// Gets the number of retry attempts made.
    /// </summary>
    public int RetryCount { get; init; }
}
