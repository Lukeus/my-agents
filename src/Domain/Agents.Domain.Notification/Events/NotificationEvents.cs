using Agents.Domain.Core.Events;

namespace Agents.Domain.Notification.Events;

/// <summary>
/// Event raised when a notification is formatted
/// </summary>
public class NotificationFormattedEvent : DomainEvent
{
    public required string NotificationId { get; init; }
    public required string Channel { get; init; }
    public required string FormattedContent { get; init; }
    public required string Recipient { get; init; }
}

/// <summary>
/// Event raised when a notification is sent
/// </summary>
public class NotificationSentEvent : DomainEvent
{
    public required string NotificationId { get; init; }
    public required string Channel { get; init; }
    public required string Recipient { get; init; }
    public required bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Event raised when a notification delivery is confirmed
/// </summary>
public class NotificationDeliveredEvent : DomainEvent
{
    public required string NotificationId { get; init; }
    public required string Channel { get; init; }
    public required DateTimeOffset DeliveredAt { get; init; }
}

/// <summary>
/// Event raised when a notification delivery fails
/// </summary>
public class NotificationFailedEvent : DomainEvent
{
    public required string NotificationId { get; init; }
    public required string Channel { get; init; }
    public required string ErrorMessage { get; init; }
    public int RetryCount { get; init; }
}
