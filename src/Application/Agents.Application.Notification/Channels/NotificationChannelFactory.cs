using Microsoft.Extensions.Logging;

namespace Agents.Application.Notification.Channels;

/// <summary>
/// Factory for creating notification channels
/// </summary>
public class NotificationChannelFactory : INotificationChannelFactory
{
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationChannelFactory"/> class.
    /// </summary>
    /// <param name="loggerFactory">The logger factory for creating channel loggers.</param>
    public NotificationChannelFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc/>
    public INotificationChannel CreateChannel(string channelName)
    {
        return channelName.ToLowerInvariant() switch
        {
            "email" => new EmailChannel(_loggerFactory.CreateLogger<EmailChannel>()),
            "sms" => new SmsChannel(_loggerFactory.CreateLogger<SmsChannel>()),
            "teams" => new TeamsChannel(_loggerFactory.CreateLogger<TeamsChannel>()),
            "slack" => new SlackChannel(_loggerFactory.CreateLogger<SlackChannel>()),
            _ => throw new ArgumentException($"Unsupported notification channel: {channelName}", nameof(channelName))
        };
    }
}
