using Agents.Domain.Core.Events;

namespace Agents.Domain.PromptRegistry.Events;

/// <summary>
/// Event raised when a new prompt is published to the registry.
/// </summary>
public class PromptPublishedEvent : DomainEvent
{
    /// <summary>
    /// Gets the prompt ID.
    /// </summary>
    public Guid PromptId { get; }

    /// <summary>
    /// Gets the prompt name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the prompt version.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// Gets the content hash of the prompt.
    /// </summary>
    public string ContentHash { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptPublishedEvent"/> class.
    /// </summary>
    /// <param name="promptId">The prompt ID.</param>
    /// <param name="name">The prompt name.</param>
    /// <param name="version">The prompt version.</param>
    /// <param name="contentHash">The content hash.</param>
    /// <param name="correlationId">Optional correlation ID.</param>
    /// <param name="causationId">Optional causation ID.</param>
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
    /// <summary>
    /// Gets the prompt ID.
    /// </summary>
    public Guid PromptId { get; }

    /// <summary>
    /// Gets the prompt name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the old version.
    /// </summary>
    public string OldVersion { get; }

    /// <summary>
    /// Gets the new version.
    /// </summary>
    public string NewVersion { get; }

    /// <summary>
    /// Gets the new content hash.
    /// </summary>
    public string NewContentHash { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptVersionUpdatedEvent"/> class.
    /// </summary>
    /// <param name="promptId">The prompt ID.</param>
    /// <param name="name">The prompt name.</param>
    /// <param name="oldVersion">The old version.</param>
    /// <param name="newVersion">The new version.</param>
    /// <param name="newContentHash">The new content hash.</param>
    /// <param name="correlationId">Optional correlation ID.</param>
    /// <param name="causationId">Optional causation ID.</param>
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
    /// <summary>
    /// Gets the prompt ID.
    /// </summary>
    public Guid PromptId { get; }

    /// <summary>
    /// Gets the prompt name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the prompt version.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// Gets the name of the prompt that replaces this one.
    /// </summary>
    public string? ReplacedBy { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptDeprecatedEvent"/> class.
    /// </summary>
    /// <param name="promptId">The prompt ID.</param>
    /// <param name="name">The prompt name.</param>
    /// <param name="version">The prompt version.</param>
    /// <param name="replacedBy">Optional name of the replacement prompt.</param>
    /// <param name="correlationId">Optional correlation ID.</param>
    /// <param name="causationId">Optional causation ID.</param>
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
    /// <summary>
    /// Gets the prompt ID.
    /// </summary>
    public Guid PromptId { get; }

    /// <summary>
    /// Gets the prompt name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the prompt version.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptMetadataUpdatedEvent"/> class.
    /// </summary>
    /// <param name="promptId">The prompt ID.</param>
    /// <param name="name">The prompt name.</param>
    /// <param name="version">The prompt version.</param>
    /// <param name="correlationId">Optional correlation ID.</param>
    /// <param name="causationId">Optional causation ID.</param>
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
    /// <summary>
    /// Gets the prompt ID.
    /// </summary>
    public Guid PromptId { get; }

    /// <summary>
    /// Gets the prompt name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the prompt version.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptDeletedEvent"/> class.
    /// </summary>
    /// <param name="promptId">The prompt ID.</param>
    /// <param name="name">The prompt name.</param>
    /// <param name="version">The prompt version.</param>
    /// <param name="correlationId">Optional correlation ID.</param>
    /// <param name="causationId">Optional causation ID.</param>
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
