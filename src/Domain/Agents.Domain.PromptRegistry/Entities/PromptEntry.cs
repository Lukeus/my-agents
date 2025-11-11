using Agents.Domain.Core.Entities;
using Agents.Domain.PromptRegistry.Events;

namespace Agents.Domain.PromptRegistry.Entities;

/// <summary>
/// Aggregate root for a prompt entry in the registry.
/// </summary>
public class PromptEntry : AggregateRoot<Guid>
{
    public string Name { get; private set; }
    public string Version { get; private set; }
    public string Description { get; private set; }
    public string? Author { get; private set; }
    public string ContentHash { get; private set; }
    public string? FilePath { get; private set; }
    public List<string> Tags { get; private set; }
    public bool IsDeprecated { get; private set; }
    public string? ReplacedBy { get; private set; }
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
