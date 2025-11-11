using Agents.Domain.Core.Events;

namespace Agents.Domain.PromptRegistry.Events;

/// <summary>
/// Event raised when a new prompt is published to the registry.
/// </summary>
public class PromptPublishedEvent : DomainEvent
{
    public Guid PromptId { get; }
    public string Name { get; }
    public string Version { get; }
    public string ContentHash { get; }

    public PromptPublishedEvent(
        Guid promptId,
        string name,
        string version,
        string contentHash,
        Guid? correlationId = null,
        Guid? causationId = null)
        : base(correlationId, causationId)
    {
        PromptId = promptId;
        Name = name;
        Version = version;
        ContentHash = contentHash;
    }
}

/// <summary>
/// Event raised when a prompt version is updated.
/// </summary>
public class PromptVersionUpdatedEvent : DomainEvent
{
    public Guid PromptId { get; }
    public string Name { get; }
    public string OldVersion { get; }
    public string NewVersion { get; }
    public string NewContentHash { get; }

    public PromptVersionUpdatedEvent(
        Guid promptId,
        string name,
        string oldVersion,
        string newVersion,
        string newContentHash,
        Guid? correlationId = null,
        Guid? causationId = null)
        : base(correlationId, causationId)
    {
        PromptId = promptId;
        Name = name;
        OldVersion = oldVersion;
        NewVersion = newVersion;
        NewContentHash = newContentHash;
    }
}

/// <summary>
/// Event raised when a prompt is deprecated.
/// </summary>
public class PromptDeprecatedEvent : DomainEvent
{
    public Guid PromptId { get; }
    public string Name { get; }
    public string Version { get; }
    public string? ReplacedBy { get; }

    public PromptDeprecatedEvent(
        Guid promptId,
        string name,
        string version,
        string? replacedBy,
        Guid? correlationId = null,
        Guid? causationId = null)
        : base(correlationId, causationId)
    {
        PromptId = promptId;
        Name = name;
        Version = version;
        ReplacedBy = replacedBy;
    }
}

/// <summary>
/// Event raised when prompt metadata is updated.
/// </summary>
public class PromptMetadataUpdatedEvent : DomainEvent
{
    public Guid PromptId { get; }
    public string Name { get; }
    public string Version { get; }

    public PromptMetadataUpdatedEvent(
        Guid promptId,
        string name,
        string version,
        Guid? correlationId = null,
        Guid? causationId = null)
        : base(correlationId, causationId)
    {
        PromptId = promptId;
        Name = name;
        Version = version;
    }
}

/// <summary>
/// Event raised when a prompt is deleted from the registry.
/// </summary>
public class PromptDeletedEvent : DomainEvent
{
    public Guid PromptId { get; }
    public string Name { get; }
    public string Version { get; }

    public PromptDeletedEvent(
        Guid promptId,
        string name,
        string version,
        Guid? correlationId = null,
        Guid? causationId = null)
        : base(correlationId, causationId)
    {
        PromptId = promptId;
        Name = name;
        Version = version;
    }
}
