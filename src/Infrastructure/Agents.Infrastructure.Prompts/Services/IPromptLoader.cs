using Agents.Infrastructure.Prompts.Models;

namespace Agents.Infrastructure.Prompts.Services;

/// <summary>
/// Interface for loading prompts from various sources
/// </summary>
public interface IPromptLoader
{
    /// <summary>
    /// Loads a single prompt from a file path
    /// </summary>
    Task<Prompt> LoadPromptAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads all prompts from a directory recursively
    /// </summary>
    Task<List<Prompt>> LoadPromptsFromDirectoryAsync(
        string directoryPath,
        string searchPattern = "*.prompt",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Watches a directory for prompt file changes
    /// </summary>
    IDisposable WatchDirectory(
        string directoryPath,
        Action<string> onChanged,
        Action<string> onCreated,
        Action<string> onDeleted);
}
