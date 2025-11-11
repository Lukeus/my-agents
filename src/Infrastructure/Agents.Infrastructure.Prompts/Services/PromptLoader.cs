using System.Security.Cryptography;
using System.Text;
using Agents.Infrastructure.Prompts.Models;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Agents.Infrastructure.Prompts.Services;

/// <summary>
/// Loads and parses prompt files from the filesystem.
/// </summary>
public class PromptLoader : IPromptLoader
{
    private readonly ILogger<PromptLoader> _logger;
    private readonly IDeserializer _yamlDeserializer;

    public PromptLoader(ILogger<PromptLoader> logger)
    {
        _logger = logger;

        _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    /// <summary>
    /// Loads a single prompt from a file path.
    /// </summary>
    public async Task<Prompt> LoadPromptAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Prompt file not found: {filePath}");
        }

        _logger.LogDebug("Loading prompt from {FilePath}", filePath);

        var content = await File.ReadAllTextAsync(filePath, cancellationToken);

        var (metadata, promptContent) = ParsePromptFile(content);

        var prompt = new Prompt
        {
            Metadata = metadata,
            Content = promptContent,
            FilePath = filePath,
            ContentHash = ComputeHash(content),
            LoadedAt = DateTime.UtcNow
        };

        _logger.LogInformation(
            "Loaded prompt '{Name}' v{Version} from {FilePath}",
            prompt.Metadata.Name,
            prompt.Metadata.Version,
            filePath);

        return prompt;
    }

    /// <summary>
    /// Loads all prompts from a directory recursively.
    /// </summary>
    public async Task<List<Prompt>> LoadPromptsFromDirectoryAsync(
        string directoryPath,
        string searchPattern = "*.prompt",
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"Prompt directory not found: {directoryPath}");
        }

        _logger.LogInformation("Loading prompts from directory {DirectoryPath}", directoryPath);

        var promptFiles = Directory.GetFiles(directoryPath, searchPattern, SearchOption.AllDirectories);
        var prompts = new List<Prompt>();

        foreach (var file in promptFiles)
        {
            try
            {
                var prompt = await LoadPromptAsync(file, cancellationToken);
                prompts.Add(prompt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load prompt from {FilePath}", file);
            }
        }

        _logger.LogInformation("Loaded {Count} prompts from {DirectoryPath}", prompts.Count, directoryPath);

        return prompts;
    }

    /// <summary>
    /// Parses a prompt file with YAML frontmatter.
    /// </summary>
    /// <returns>Tuple of (metadata, content)</returns>
    private (PromptMetadata metadata, string content) ParsePromptFile(string fileContent)
    {
        const string yamlDelimiter = "---";

        // Check if file starts with YAML frontmatter
        if (!fileContent.TrimStart().StartsWith(yamlDelimiter))
        {
            throw new InvalidOperationException("Prompt file must start with YAML frontmatter (---)")
;
        }

        var lines = fileContent.Split('\n');
        var yamlLines = new List<string>();
        var contentLines = new List<string>();
        var inYaml = false;
        var yamlBlockCount = 0;

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            if (trimmedLine == yamlDelimiter)
            {
                yamlBlockCount++;
                if (yamlBlockCount == 1)
                {
                    inYaml = true;
                    continue;
                }
                else if (yamlBlockCount == 2)
                {
                    inYaml = false;
                    continue;
                }
            }

            if (inYaml)
            {
                yamlLines.Add(line);
            }
            else if (yamlBlockCount >= 2)
            {
                contentLines.Add(line);
            }
        }

        if (yamlBlockCount < 2)
        {
            throw new InvalidOperationException("Prompt file must have closing YAML frontmatter delimiter (---)");
        }

        var yamlContent = string.Join("\n", yamlLines);
        var promptContent = string.Join("\n", contentLines).Trim();

        var metadata = _yamlDeserializer.Deserialize<PromptMetadata>(yamlContent);

        if (metadata == null)
        {
            throw new InvalidOperationException("Failed to parse prompt metadata from YAML frontmatter");
        }

        return (metadata, promptContent);
    }

    /// <summary>
    /// Computes SHA-256 hash of content for change detection.
    /// </summary>
    private static string ComputeHash(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Watches a directory for prompt file changes.
    /// </summary>
    public IDisposable WatchDirectory(
        string directoryPath,
        Action<string> onChanged,
        Action<string> onCreated,
        Action<string> onDeleted)
    {
        var watcher = new FileSystemWatcher(directoryPath)
        {
            Filter = "*.prompt",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime,
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };

        watcher.Changed += (sender, e) =>
        {
            _logger.LogInformation("Prompt file changed: {FilePath}", e.FullPath);
            onChanged(e.FullPath);
        };

        watcher.Created += (sender, e) =>
        {
            _logger.LogInformation("Prompt file created: {FilePath}", e.FullPath);
            onCreated(e.FullPath);
        };

        watcher.Deleted += (sender, e) =>
        {
            _logger.LogInformation("Prompt file deleted: {FilePath}", e.FullPath);
            onDeleted(e.FullPath);
        };

        return watcher;
    }
}
