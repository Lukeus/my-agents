using Microsoft.Extensions.Logging;

namespace Agents.Application.Notification.Channels;

/// <summary>
/// SMS notification channel (placeholder - would integrate with Twilio, Azure Communication Services, etc.)
/// </summary>
public class SmsChannel : INotificationChannel
{
    private readonly ILogger<SmsChannel> _logger;

    public string ChannelName => "SMS";

    public SmsChannel(ILogger<SmsChannel> logger)
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
            _logger.LogInformation("Sending SMS to {Recipient}", recipient);

            // TODO: Integrate with SMS service (Twilio, Azure Communication Services, etc.)
            await Task.Delay(100, cancellationToken);

            var messageId = Guid.NewGuid().ToString();
            _logger.LogInformation("SMS sent successfully. MessageId: {MessageId}", messageId);

            return ChannelResult.Success(messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS to {Recipient}", recipient);
            return ChannelResult.Failure(ex.Message);
        }
    }
}
