using Microsoft.Extensions.Logging;

namespace Agents.Application.Notification.Channels;

/// <summary>
/// Email notification channel (placeholder - would integrate with SendGrid, SMTP, etc.)
/// </summary>
public class EmailChannel : INotificationChannel
{
    private readonly ILogger<EmailChannel> _logger;

    public string ChannelName => "Email";

    public EmailChannel(ILogger<EmailChannel> logger)
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
            _logger.LogInformation("Sending email to {Recipient} with subject: {Subject}", recipient, subject);

            // TODO: Integrate with actual email service (SendGrid, Azure Communication Services, etc.)
            await Task.Delay(100, cancellationToken); // Simulate async operation

            var messageId = Guid.NewGuid().ToString();
            _logger.LogInformation("Email sent successfully. MessageId: {MessageId}", messageId);

            return ChannelResult.Success(messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient}", recipient);
            return ChannelResult.Failure(ex.Message);
        }
    }
}
