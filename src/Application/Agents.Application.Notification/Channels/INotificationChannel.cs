namespace Agents.Application.Notification.Channels;

/// <summary>
/// Interface for notification delivery channels
/// </summary>
public interface INotificationChannel
{
    string ChannelName { get; }
    Task<ChannelResult> SendAsync(string recipient, string subject, string content, CancellationToken cancellationToken = default);
}

/// <summary>
/// Factory for creating notification channels
/// </summary>
public interface INotificationChannelFactory
{
    INotificationChannel CreateChannel(string channelName);
}

/// <summary>
/// Result from a channel send operation
/// </summary>
public record ChannelResult
{
    public bool IsSuccess { get; init; }
    public string? MessageId { get; init; }
    public string? ErrorMessage { get; init; }

    public static ChannelResult Success(string? messageId = null) =>
        new() { IsSuccess = true, MessageId = messageId };

    public static ChannelResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}
