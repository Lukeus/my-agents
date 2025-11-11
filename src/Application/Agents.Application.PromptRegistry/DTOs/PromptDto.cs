namespace Agents.Application.PromptRegistry.DTOs;

/// <summary>
/// Data transfer object for a prompt.
/// </summary>
public class PromptDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Version { get; set; }
    public required string Description { get; set; }
    public string? Author { get; set; }
    public required string ContentHash { get; set; }
    public string? FilePath { get; set; }
    public List<string> Tags { get; set; } = new();
    public bool IsDeprecated { get; set; }
    public string? ReplacedBy { get; set; }
    public DateTime PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Data transfer object for a prompt version.
/// </summary>
public class PromptVersionDto
{
    public required string Version { get; set; }
    public required string ContentHash { get; set; }
    public DateTime PublishedAt { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Paged result wrapper.
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
