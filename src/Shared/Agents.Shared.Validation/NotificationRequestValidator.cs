using FluentValidation;

namespace Agents.Shared.Validation;

// Note: This is a generic validator base. Specific validators should be
// created in the Application layer where the actual request models are defined.

/// <summary>
/// Base validator providing common validation rules for notification-like requests
/// </summary>
/// <typeparam name="T">The notification request type</typeparam>
public abstract class NotificationRequestValidatorBase<T> : AbstractValidator<T> where T : INotificationRequest
{
    private static readonly string[] AllowedChannels = { "email", "sms", "teams", "slack" };

    protected NotificationRequestValidatorBase()
    {
        RuleFor(x => x.Channel)
            .NotEmpty()
            .WithMessage("Channel is required")
            .Must(channel => AllowedChannels.Contains(channel.ToLower()))
            .WithMessage($"Channel must be one of: {string.Join(", ", AllowedChannels)}");

        RuleFor(x => x.Recipient)
            .NotEmpty()
            .WithMessage("Recipient is required")
            .MaximumLength(500)
            .WithMessage("Recipient must not exceed 500 characters")
            .EmailAddress()
            .When(x => x.Channel.Equals("email", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Recipient must be a valid email address for email channel");

        RuleFor(x => x.Subject)
            .NotEmpty()
            .WithMessage("Subject is required")
            .MaximumLength(500)
            .WithMessage("Subject must not exceed 500 characters")
            .Must(subject => !ContainsInjectionPatterns(subject))
            .WithMessage("Subject contains potentially malicious content");

        RuleFor(x => x.Content)
            .NotEmpty()
            .WithMessage("Content is required")
            .MaximumLength(10000)
            .WithMessage("Content must not exceed 10,000 characters")
            .Must(content => !ContainsInjectionPatterns(content))
            .WithMessage("Content contains potentially malicious content");
    }

    /// <summary>
    /// Basic check for common injection patterns
    /// </summary>
    private static bool ContainsInjectionPatterns(string input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        var dangerousPatterns = new[]
        {
            "<script",
            "javascript:",
            "onerror=",
            "onclick=",
            "SYSTEM:",
            "INSTRUCTION:",
            "IGNORE PREVIOUS",
            "DISREGARD ALL"
        };

        return dangerousPatterns.Any(pattern =>
            input.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Interface for notification requests to enable validation
/// </summary>
public interface INotificationRequest
{
    string Channel { get; }
    string Recipient { get; }
    string Subject { get; }
    string Content { get; }
}
