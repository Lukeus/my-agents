using System.Security.Cryptography;
using System.Text;
using Agents.Infrastructure.Prompts.Models;
using Microsoft.Extensions.Caching.Memory;
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
    private readonly IMemoryCache _cache;
    private readonly IDeserializer _yamlDeserializer;
    private readonly SemaphoreSlim _cacheLock = new(1, 1);
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);

    public PromptLoader(ILogger<PromptLoader> logger, IMemoryCache cache)
    {
        _logger = logger;
        _cache = cache;

        _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    /// <summary>
    /// Loads a single prompt from a file path with caching.
    /// </summary>
    public async Task<Prompt> LoadPromptAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Prompt file not found: {filePath}");
        }

        var cacheKey = $"prompt:{filePath}";

        // Try to get from cache first
        if (_cache.TryGetValue<Prompt>(cacheKey, out var cachedPrompt) && cachedPrompt != null)
        {
            _logger.LogDebug("Prompt loaded from cache: {FilePath}", filePath);
            return cachedPrompt;
        }

        // Lock to prevent multiple simultaneous loads of the same file
        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check cache after acquiring lock
            if (_cache.TryGetValue<Prompt>(cacheKey, out cachedPrompt) && cachedPrompt != null)
            {
                return cachedPrompt;
            }

            _logger.LogDebug("Loading prompt from file: {FilePath}", filePath);

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

            // Cache with sliding expiration
            var cacheOptions = new MemoryCacheEntryOptions
            {
                SlidingExpiration = CacheDuration,
                Size = 1 // Each prompt counts as 1 unit
            };

            _cache.Set(cacheKey, prompt, cacheOptions);

            _logger.LogInformation(
                "Loaded and cached prompt '{Name}' v{Version} from {FilePath}",
                prompt.Metadata.Name,
                prompt.Metadata.Version,
                filePath);

            return prompt;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    /// <summary>
    /// Loads all prompts from a directory recursively with parallel loading.
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

        var cacheKey = $"prompts:dir:{directoryPath}:{searchPattern}";

        // Check if the entire directory result is cached
        if (_cache.TryGetValue<List<Prompt>>(cacheKey, out var cachedPrompts) && cachedPrompts != null)
        {
            _logger.LogDebug("Prompts loaded from cache for directory: {DirectoryPath}", directoryPath);
            return cachedPrompts;
        }

        _logger.LogInformation("Loading prompts from directory {DirectoryPath}", directoryPath);

        var promptFiles = Directory.GetFiles(directoryPath, searchPattern, SearchOption.AllDirectories);

        // Parallel loading for better performance
        var loadTasks = promptFiles.Select(async file =>
        {
            try
            {
                return await LoadPromptAsync(file, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load prompt from {FilePath}", file);
                return null;
            }
        });

        var results = await Task.WhenAll(loadTasks);
        var prompts = results.Where(p => p != null).Cast<Prompt>().ToList();

        // Cache the directory results
        var cacheOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = CacheDuration,
            Size = prompts.Count
        };
        _cache.Set(cacheKey, prompts, cacheOptions);

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
            // Invalidate cache for this file
            _cache.Remove($"prompt:{e.FullPath}");
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
            // Invalidate cache for this file
            _cache.Remove($"prompt:{e.FullPath}");
            onDeleted(e.FullPath);
        };

        return watcher;
    }
}
