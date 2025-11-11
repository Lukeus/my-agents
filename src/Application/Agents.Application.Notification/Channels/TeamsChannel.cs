using Microsoft.Extensions.Logging;

namespace Agents.Application.Notification.Channels;

/// <summary>
/// Microsoft Teams notification channel (placeholder - would integrate with Teams webhooks/Graph API)
/// </summary>
public class TeamsChannel : INotificationChannel
{
    private readonly ILogger<TeamsChannel> _logger;

    public string ChannelName => "Teams";

    public TeamsChannel(ILogger<TeamsChannel> logger)
    {
        _logger = logger;
    }

    public async Task<ChannelResult> SendAsync(
        string recipient,
        string subject,
        string content,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending Teams message to {Recipient}", recipient);

            // TODO: Integrate with Microsoft Teams (Webhook or Graph API)
            await Task.Delay(100, cancellationToken);

            var messageId = Guid.NewGuid().ToString();
            _logger.LogInformation("Teams message sent successfully. MessageId: {MessageId}", messageId);

            return ChannelResult.Success(messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Teams message to {Recipient}", recipient);
            return ChannelResult.Failure(ex.Message);
        }
    }
}
