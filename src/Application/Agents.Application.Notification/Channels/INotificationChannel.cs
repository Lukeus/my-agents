namespace Agents.Application.Notification.Channels;

/// <summary>
/// Interface for notification delivery channels.
/// </summary>
public interface INotificationChannel
{
    /// <summary>
    /// Gets the name of the notification channel (e.g., "email", "sms", "teams", "slack").
    /// </summary>
    string ChannelName { get; }

    /// <summary>
    /// Sends a notification through this channel.
    /// </summary>
    /// <param name="recipient">The recipient identifier (email, phone, user ID, etc.).</param>
    /// <param name="subject">The notification subject or title.</param>
    /// <param name="content">The notification content.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the send operation.</returns>
    Task<ChannelResult> SendAsync(string recipient, string subject, string content, CancellationToken cancellationToken = default);
}

/// <summary>
/// Factory for creating notification channels.
/// </summary>
public interface INotificationChannelFactory
{
    /// <summary>
    /// Creates a notification channel by name.
    /// </summary>
    /// <param name="channelName">The channel name (e.g., "email", "sms", "teams", "slack").</param>
    /// <returns>The notification channel instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the channel name is not supported.</exception>
    INotificationChannel CreateChannel(string channelName);
}

/// <summary>
/// Result from a channel send operation.
/// </summary>
public record ChannelResult
{
    /// <summary>
    /// Gets a value indicating whether the send operation was successful.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets the message ID returned by the channel provider.
    /// </summary>
    public string? MessageId { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful channel result.
    /// </summary>
    /// <param name="messageId">Optional message ID from the channel provider.</param>
    /// <returns>A successful channel result.</returns>
    public static ChannelResult Success(string? messageId = null) =>
        new() { IsSuccess = true, MessageId = messageId };

    /// <summary>
    /// Creates a failed channel result.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A failed channel result.</returns>
    public static ChannelResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}
