namespace Agents.Application.PromptRegistry.DTOs;

/// <summary>
/// Data transfer object for a prompt.
/// </summary>
public class PromptDto
{
    /// <summary>
    /// Gets or sets the prompt ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the prompt name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the prompt version.
    /// </summary>
    public required string Version { get; set; }

    /// <summary>
    /// Gets or sets the prompt description.
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Gets or sets the author.
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// Gets or sets the content hash.
    /// </summary>
    public required string ContentHash { get; set; }

    /// <summary>
    /// Gets or sets the file path.
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// Gets or sets the tags.
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether the prompt is deprecated.
    /// </summary>
    public bool IsDeprecated { get; set; }

    /// <summary>
    /// Gets or sets the name of the replacement prompt if deprecated.
    /// </summary>
    public string? ReplacedBy { get; set; }

    /// <summary>
    /// Gets or sets the published timestamp.
    /// </summary>
    public DateTime PublishedAt { get; set; }

    /// <summary>
    /// Gets or sets the created timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the updated timestamp.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Data transfer object for a prompt version.
/// </summary>
public class PromptVersionDto
{
    /// <summary>
    /// Gets or sets the version.
    /// </summary>
    public required string Version { get; set; }

    /// <summary>
    /// Gets or sets the content hash.
    /// </summary>
    public required string ContentHash { get; set; }

    /// <summary>
    /// Gets or sets the published timestamp.
    /// </summary>
    public DateTime PublishedAt { get; set; }

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Paged result wrapper.
/// </summary>
/// <typeparam name="T">The type of items in the result.</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// Gets or sets the items in the current page.
    /// </summary>
    public List<T> Items { get; set; } = new();

    /// <summary>
    /// Gets or sets the total count of items across all pages.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the current page number.
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>
    /// Gets a value indicating whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Gets a value indicating whether there is a next page.
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;
}
