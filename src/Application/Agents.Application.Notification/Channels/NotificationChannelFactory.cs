using Microsoft.Extensions.Logging;

namespace Agents.Application.Notification.Channels;

/// <summary>
/// Factory for creating notification channels
/// </summary>
public class NotificationChannelFactory : INotificationChannelFactory
{
    private readonly ILoggerFactory _loggerFactory;

    public NotificationChannelFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

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
