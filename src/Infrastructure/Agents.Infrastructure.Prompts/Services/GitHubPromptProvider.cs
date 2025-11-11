using Agents.Infrastructure.Prompts.Models;
using Microsoft.Extensions.Logging;
using Octokit;

namespace Agents.Infrastructure.Prompts.Services;

/// <summary>
/// Syncs and loads prompts from a GitHub repository.
/// </summary>
public class GitHubPromptProvider
{
    private readonly GitHubClient _client;
    private readonly PromptLoader _promptLoader;
    private readonly ILogger<GitHubPromptProvider> _logger;

    public GitHubPromptProvider(
        string productHeader,
        PromptLoader promptLoader,
        ILogger<GitHubPromptProvider> logger,
        string? accessToken = null)
    {
        _promptLoader = promptLoader;
        _logger = logger;

        _client = new GitHubClient(new ProductHeaderValue(productHeader));

        if (!string.IsNullOrEmpty(accessToken))
        {
            _client.Credentials = new Credentials(accessToken);
        }
    }

    /// <summary>
    /// Lists all prompt files in a GitHub repository.
    /// </summary>
    public async Task<List<RepositoryContent>> ListPromptsAsync(
        string owner,
        string repo,
        string path = "prompts",
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Listing prompts from GitHub: {Owner}/{Repo}/{Path}",
                owner,
                repo,
                path);

            var contents = await _client.Repository.Content.GetAllContents(owner, repo, path);

            var promptFiles = new List<RepositoryContent>();

            foreach (var content in contents)
            {
                if (content.Type == ContentType.File && content.Name.EndsWith(".prompt"))
                {
                    promptFiles.Add(content);
                }
                else if (content.Type == ContentType.Dir)
                {
                    // Recursively get prompts from subdirectories
                    var subPrompts = await ListPromptsAsync(owner, repo, content.Path, cancellationToken);
                    promptFiles.AddRange(subPrompts);
                }
            }

            _logger.LogInformation("Found {Count} prompt files in GitHub repository", promptFiles.Count);

            return promptFiles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list prompts from GitHub");
            throw;
        }
    }

    /// <summary>
    /// Downloads and parses a prompt file from GitHub.
    /// </summary>
    public async Task<Prompt> GetPromptAsync(
        string owner,
        string repo,
        string path,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Downloading prompt from GitHub: {Owner}/{Repo}/{Path}", owner, repo, path);

            var contents = await _client.Repository.Content.GetAllContents(owner, repo, path);
            var file = contents.FirstOrDefault();

            if (file == null)
            {
                throw new FileNotFoundException($"Prompt file not found: {path}");
            }

            var content = file.Content;

            // Parse the prompt content
            var (metadata, promptContent) = ParsePromptContent(content);

            var prompt = new Prompt
            {
                Metadata = metadata,
                Content = promptContent,
                FilePath = $"github://{owner}/{repo}/{path}",
                ContentHash = file.Sha,
                LoadedAt = DateTime.UtcNow
            };

            _logger.LogInformation(
                "Downloaded prompt '{Name}' v{Version} from GitHub",
                prompt.Metadata.Name,
                prompt.Metadata.Version);

            return prompt;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download prompt from GitHub");
            throw;
        }
    }

    /// <summary>
    /// Syncs all prompts from a GitHub repository to local directory.
    /// </summary>
    public async Task<int> SyncPromptsAsync(
        string owner,
        string repo,
        string localDirectory,
        string remotePath = "prompts",
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Syncing prompts from GitHub {Owner}/{Repo} to {LocalDirectory}",
            owner,
            repo,
            localDirectory);

        var promptFiles = await ListPromptsAsync(owner, repo, remotePath, cancellationToken);
        var syncedCount = 0;

        foreach (var file in promptFiles)
        {
            try
            {
                var prompt = await GetPromptAsync(owner, repo, file.Path, cancellationToken);

                // Determine local file path
                var relativePath = file.Path.StartsWith(remotePath)
                    ? file.Path.Substring(remotePath.Length).TrimStart('/')
                    : file.Path;

                var localPath = Path.Combine(localDirectory, relativePath);
                var localDir = Path.GetDirectoryName(localPath);

                if (localDir != null && !Directory.Exists(localDir))
                {
                    Directory.CreateDirectory(localDir);
                }

                // Write content to local file
                var fullContent = $"---\n{SerializeMetadata(prompt.Metadata)}\n---\n{prompt.Content}";
                await File.WriteAllTextAsync(localPath, fullContent, cancellationToken);

                _logger.LogDebug("Synced prompt to {LocalPath}", localPath);
                syncedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync prompt {Path}", file.Path);
            }
        }

        _logger.LogInformation("Synced {Count} prompts from GitHub", syncedCount);

        return syncedCount;
    }

    /// <summary>
    /// Watches a GitHub repository for changes using polling.
    /// </summary>
    public Task<IDisposable> WatchRepositoryAsync(
        string owner,
        string repo,
        string path,
        TimeSpan pollInterval,
        Func<List<RepositoryContent>, Task> onChange,
        CancellationToken cancellationToken = default)
    {
        var timer = new System.Timers.Timer(pollInterval.TotalMilliseconds);
        var lastCommitSha = string.Empty;

        timer.Elapsed += async (sender, args) =>
        {
            try
            {
                // Get latest commit
                var commits = await _client.Repository.Commit.GetAll(owner, repo, new CommitRequest
                {
                    Path = path
                });

                var latestCommit = commits.FirstOrDefault();
                if (latestCommit != null && latestCommit.Sha != lastCommitSha)
                {
                    _logger.LogInformation(
                        "Detected changes in GitHub repository {Owner}/{Repo}",
                        owner,
                        repo);

                    lastCommitSha = latestCommit.Sha;

                    var promptFiles = await ListPromptsAsync(owner, repo, path, cancellationToken);
                    await onChange(promptFiles);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error watching GitHub repository");
            }
        };

        timer.Start();

        return Task.FromResult<IDisposable>(timer);
    }

    private (PromptMetadata metadata, string content) ParsePromptContent(string fileContent)
    {
        // Reuse the parsing logic from PromptLoader
        // This is a simplified version - in production, should extract to shared utility
        const string yamlDelimiter = "---";

        if (!fileContent.TrimStart().StartsWith(yamlDelimiter))
        {
            throw new InvalidOperationException("Prompt file must start with YAML frontmatter (---)");
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

        var yamlContent = string.Join("\n", yamlLines);
        var promptContent = string.Join("\n", contentLines).Trim();

        var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
            .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var metadata = deserializer.Deserialize<PromptMetadata>(yamlContent);

        if (metadata == null)
        {
            throw new InvalidOperationException("Failed to parse prompt metadata");
        }

        return (metadata, promptContent);
    }

    private string SerializeMetadata(PromptMetadata metadata)
    {
        var serializer = new YamlDotNet.Serialization.SerializerBuilder()
            .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.UnderscoredNamingConvention.Instance)
            .Build();

        return serializer.Serialize(metadata);
    }
}
