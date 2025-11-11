using MediatR;

namespace Agents.Application.PromptRegistry.Commands;

/// <summary>
/// Command to publish a new prompt to the registry.
/// </summary>
public record PublishPromptCommand(
    string Name,
    string Version,
    string Description,
    string ContentHash,
    string? Author = null,
    List<string>? Tags = null) : IRequest<Guid>;

/// <summary>
/// Command to update a prompt version.
/// </summary>
public record UpdatePromptVersionCommand(
    Guid PromptId,
    string NewVersion,
    string NewContentHash,
    string? NewDescription = null) : IRequest;

/// <summary>
/// Command to deprecate a prompt.
/// </summary>
public record DeprecatePromptCommand(
    Guid PromptId,
    string? ReplacedBy = null) : IRequest;

/// <summary>
/// Command to update prompt metadata.
/// </summary>
public record UpdatePromptMetadataCommand(
    Guid PromptId,
    string? Description = null,
    string? Author = null,
    List<string>? Tags = null) : IRequest;

/// <summary>
/// Command to delete a prompt from the registry.
/// </summary>
public record DeletePromptCommand(Guid PromptId) : IRequest;
