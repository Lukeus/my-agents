using Agents.Domain.Core.Entities;
using Agents.Domain.PromptRegistry.Events;

namespace Agents.Domain.PromptRegistry.Entities;

/// <summary>
/// Aggregate root for a prompt entry in the registry.
/// </summary>
public class PromptEntry : AggregateRoot<Guid>
{
    /// <summary>
    /// Gets the unique name of the prompt.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the semantic version of the prompt (e.g., "1.0.0").
    /// </summary>
    public string Version { get; private set; }

    /// <summary>
    /// Gets the human-readable description of the prompt's purpose.
    /// </summary>
    public string Description { get; private set; }

    /// <summary>
    /// Gets the author of the prompt.
    /// </summary>
    public string? Author { get; private set; }

    /// <summary>
    /// Gets the SHA-256 hash of the prompt content for change detection.
    /// </summary>
    public string ContentHash { get; private set; }

    /// <summary>
    /// Gets the file path where the prompt is stored.
    /// </summary>
    public string? FilePath { get; private set; }

    /// <summary>
    /// Gets the tags for categorization and search.
    /// </summary>
    public List<string> Tags { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this prompt is deprecated.
    /// </summary>
    public bool IsDeprecated { get; private set; }

    /// <summary>
    /// Gets the name of the prompt that replaces this one if deprecated.
    /// </summary>
    public string? ReplacedBy { get; private set; }

    /// <summary>
    /// Gets the timestamp when the prompt was published to the registry.
    /// </summary>
    public DateTime PublishedAt { get; private set; }

    private PromptEntry(Guid id) : base(id)
    {
        Name = string.Empty;
        Version = string.Empty;
        Description = string.Empty;
        ContentHash = string.Empty;
        Tags = new List<string>();
        PublishedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a new prompt entry.
    /// </summary>
    public static PromptEntry Create(
        string name,
        string version,
        string description,
        string contentHash,
        string? author = null,
        List<string>? tags = null)
    {
        var entry = new PromptEntry(Guid.NewGuid())
        {
            Name = name,
            Version = version,
            Description = description,
            ContentHash = contentHash,
            Author = author,
            Tags = tags ?? new List<string>(),
            PublishedAt = DateTime.UtcNow
        };

        entry.AddDomainEvent(new PromptPublishedEvent(
            entry.Id,
            entry.Name,
            entry.Version,
            entry.ContentHash));

        return entry;
    }

    /// <summary>
    /// Updates the prompt entry with new version.
    /// </summary>
    public void UpdateVersion(string newVersion, string newContentHash, string? newDescription = null)
    {
        var oldVersion = Version;

        Version = newVersion;
        ContentHash = newContentHash;

        if (newDescription != null)
        {
            Description = newDescription;
        }

        MarkAsUpdated();

        AddDomainEvent(new PromptVersionUpdatedEvent(
            Id,
            Name,
            oldVersion,
            newVersion,
            newContentHash));
    }

    /// <summary>
    /// Marks the prompt as deprecated.
    /// </summary>
    public void Deprecate(string? replacedBy = null)
    {
        if (IsDeprecated)
        {
            return; // Already deprecated
        }

        IsDeprecated = true;
        ReplacedBy = replacedBy;
        MarkAsUpdated();

        AddDomainEvent(new PromptDeprecatedEvent(
            Id,
            Name,
            Version,
            replacedBy));
    }

    /// <summary>
    /// Updates metadata (tags, author, description).
    /// </summary>
    public void UpdateMetadata(string? description = null, string? author = null, List<string>? tags = null)
    {
        if (description != null)
        {
            Description = description;
        }

        if (author != null)
        {
            Author = author;
        }

        if (tags != null)
        {
            Tags = tags;
        }

        MarkAsUpdated();

        AddDomainEvent(new PromptMetadataUpdatedEvent(
            Id,
            Name,
            Version));
    }
}
