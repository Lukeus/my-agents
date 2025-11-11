using Agents.Application.Core;
using Agents.Application.Notification.Channels;
using Agents.Domain.Notification.Entities;
using Agents.Domain.Notification.Events;
using Agents.Domain.Core.Interfaces;
using Agents.Infrastructure.Prompts.Services;
using Microsoft.Extensions.Logging;

namespace Agents.Application.Notification;

/// <summary>
/// Agent responsible for formatting and sending notifications across multiple channels
/// </summary>
public class NotificationAgent : BaseAgent
{
    private readonly INotificationChannelFactory _channelFactory;

    public NotificationAgent(
        ILLMProvider llmProvider,
        IPromptLoader promptLoader,
        IEventPublisher eventPublisher,
        INotificationChannelFactory channelFactory,
        ILogger<NotificationAgent> logger)
        : base(llmProvider, promptLoader, eventPublisher, logger, "NotificationAgent")
    {
        _channelFactory = channelFactory ?? throw new ArgumentNullException(nameof(channelFactory));
    }

    protected override async Task<AgentResult> ExecuteCoreAsync(string input, AgentContext context)
    {
        try
        {
            // Parse notification request from input
            var notificationRequest = System.Text.Json.JsonSerializer.Deserialize<NotificationRequest>(input);
            if (notificationRequest == null)
            {
                return AgentResult.Failure("Invalid notification request format");
            }

            var notification = new Domain.Notification.Entities.Notification(
                Guid.NewGuid().ToString(),
                notificationRequest.Channel,
                notificationRequest.Recipient,
                notificationRequest.Subject,
                notificationRequest.Content);

            // Format notification using LLM
            var formattedContent = await FormatNotificationAsync(
                notificationRequest.Channel,
                notificationRequest.Subject,
                notificationRequest.Content,
                context.CancellationToken);

            notification.MarkAsFormatted(formattedContent);
            await PublishEventAsync(notification.DomainEvents.OfType<NotificationFormattedEvent>().First(), context.CancellationToken);

            // Send notification through appropriate channel
            var channel = _channelFactory.CreateChannel(notificationRequest.Channel);
            var sendResult = await channel.SendAsync(
                notificationRequest.Recipient,
                notificationRequest.Subject,
                formattedContent,
                context.CancellationToken);

            if (sendResult.IsSuccess)
            {
                notification.MarkAsSent();
                await PublishEventAsync(notification.DomainEvents.OfType<NotificationSentEvent>().Last(), context.CancellationToken);

                return AgentResult.Success(
                    $"Notification sent successfully via {notificationRequest.Channel}",
                    new Dictionary<string, object>
                    {
                        ["NotificationId"] = notification.Id,
                        ["Channel"] = notificationRequest.Channel,
                        ["Recipient"] = notificationRequest.Recipient
                    });
            }
            else
            {
                notification.MarkAsFailed(sendResult.ErrorMessage ?? "Unknown error");
                await PublishEventAsync(notification.DomainEvents.OfType<NotificationFailedEvent>().Last(), context.CancellationToken);

                return AgentResult.Failure(
                    $"Failed to send notification: {sendResult.ErrorMessage}",
                    new Dictionary<string, object>
                    {
                        ["NotificationId"] = notification.Id,
                        ["CanRetry"] = notification.CanRetry()
                    });
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing notification");
            return AgentResult.Failure($"Error processing notification: {ex.Message}");
        }
    }

    private async Task<string> FormatNotificationAsync(
        string channel,
        string subject,
        string content,
        CancellationToken cancellationToken)
    {
        try
        {
            var promptName = $"prompts/notification-{channel.ToLower()}-formatter.prompt";
            var promptText = await LoadPromptAsync(promptName, new Dictionary<string, object>
            {
                ["subject"] = subject,
                ["content"] = content
            });

            var formattedContent = await InvokeKernelAsync(promptText, cancellationToken: cancellationToken);
            return formattedContent;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to format with LLM, using default formatting");
            return $"{subject}\n\n{content}";
        }
    }
}

/// <summary>
/// Notification request model
/// </summary>
public record NotificationRequest
{
    public required string Channel { get; init; }
    public required string Recipient { get; init; }
    public required string Subject { get; init; }
    public required string Content { get; init; }
}
