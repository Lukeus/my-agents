using Agents.Domain.Core.Events;

namespace Agents.Infrastructure.EventGrid.Models;

/// <summary>
/// Wrapper for domain events to conform to Azure Event Grid schema.
/// </summary>
/// <typeparam name="TData">The type of the event data payload.</typeparam>
public class EventGridEnvelope<TData> where TData : IDomainEvent
{
    /// <summary>
    /// Unique identifier for the event.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Event type in the format: agents.{domain}.{action}
    /// </summary>
    public required string EventType { get; set; }

    /// <summary>
    /// Context path for the event subject.
    /// </summary>
    public required string Subject { get; set; }

    /// <summary>
    /// Timestamp when the event occurred (ISO 8601).
    /// </summary>
    public required DateTime EventTime { get; set; }

    /// <summary>
    /// Schema version of the data object.
    /// </summary>
    public string DataVersion { get; set; } = "1.0";

    /// <summary>
    /// Schema version of the Event Grid metadata.
    /// </summary>
    public string MetadataVersion { get; set; } = "1";

    /// <summary>
    /// The actual domain event data.
    /// </summary>
    public required TData Data { get; set; }

    /// <summary>
    /// Creates an Event Grid envelope from a domain event.
    /// </summary>
    public static EventGridEnvelope<TData> FromDomainEvent(TData domainEvent, string subject)
    {
        var eventType = $"agents.{GetDomainFromType(typeof(TData))}.{GetActionFromType(typeof(TData))}";

        return new EventGridEnvelope<TData>
        {
            Id = domainEvent.EventId.ToString(),
            EventType = eventType,
            Subject = subject,
            EventTime = domainEvent.OccurredAt,
            Data = domainEvent
        };
    }

    private static string GetDomainFromType(Type type)
    {
        // Extract domain from namespace or type name
        // e.g., Agents.Domain.Notification.Events.NotificationSentEvent -> notification
        var namespaceParts = type.Namespace?.Split('.') ?? Array.Empty<string>();
        if (namespaceParts.Length >= 3)
        {
            return namespaceParts[2].ToLowerInvariant();
        }

        return "unknown";
    }

    private static string GetActionFromType(Type type)
    {
        // Convert PascalCase event name to lowercase with dots
        // e.g., NotificationSentEvent -> sent
        var name = type.Name;
        if (name.EndsWith("Event", StringComparison.OrdinalIgnoreCase))
        {
            name = name[..^5]; // Remove "Event" suffix
        }

        // Simple conversion - can be enhanced with regex for proper camelCase split
        return name.ToLowerInvariant();
    }
}
