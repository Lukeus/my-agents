using Agents.Application.PromptRegistry.DTOs;
using MediatR;

namespace Agents.Application.PromptRegistry.Queries;

/// <summary>
/// Query to get a prompt by ID.
/// </summary>
public record GetPromptByIdQuery(Guid PromptId) : IRequest<PromptDto?>;

/// <summary>
/// Query to get a prompt by name and version.
/// </summary>
public record GetPromptByNameQuery(
    string Name,
    string? Version = null) : IRequest<PromptDto?>;

/// <summary>
/// Query to list all prompts.
/// </summary>
public record ListPromptsQuery(
    int PageNumber = 1,
    int PageSize = 50,
    string? SearchTerm = null,
    List<string>? Tags = null,
    bool IncludeDeprecated = false) : IRequest<PagedResult<PromptDto>>;

/// <summary>
/// Query to get prompt version history.
/// </summary>
public record GetPromptVersionHistoryQuery(string Name) : IRequest<List<PromptVersionDto>>;
