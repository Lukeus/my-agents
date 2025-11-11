using Agents.Infrastructure.Prompts.Models;
using Microsoft.Extensions.Logging;

namespace Agents.Infrastructure.Prompts.Services;

/// <summary>
/// Manages prompt versions, tracking changes and version history.
/// </summary>
public class PromptVersionManager
{
    private readonly ILogger<PromptVersionManager> _logger;
    private readonly Dictionary<string, List<PromptVersion>> _versionHistory = new();

    public PromptVersionManager(ILogger<PromptVersionManager> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Registers a new version of a prompt.
    /// </summary>
    public void RegisterVersion(Prompt prompt)
    {
        var version = new PromptVersion
        {
            Name = prompt.Metadata.Name,
            Version = prompt.Metadata.Version,
            ContentHash = prompt.ContentHash ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
            FilePath = prompt.FilePath,
            Metadata = prompt.Metadata
        };

        if (!_versionHistory.ContainsKey(prompt.Metadata.Name))
        {
            _versionHistory[prompt.Metadata.Name] = new List<PromptVersion>();
        }

        _versionHistory[prompt.Metadata.Name].Add(version);

        _logger.LogInformation(
            "Registered version {Version} for prompt '{Name}'",
            version.Version,
            version.Name);
    }

    /// <summary>
    /// Gets all versions of a prompt.
    /// </summary>
    public List<PromptVersion> GetVersionHistory(string promptName)
    {
        if (_versionHistory.TryGetValue(promptName, out var versions))
        {
            return versions.OrderByDescending(v => v.CreatedAt).ToList();
        }

        return new List<PromptVersion>();
    }

    /// <summary>
    /// Gets a specific version of a prompt.
    /// </summary>
    public PromptVersion? GetVersion(string promptName, string version)
    {
        if (_versionHistory.TryGetValue(promptName, out var versions))
        {
            return versions.FirstOrDefault(v => v.Version == version);
        }

        return null;
    }

    /// <summary>
    /// Gets the latest version of a prompt.
    /// </summary>
    public PromptVersion? GetLatestVersion(string promptName)
    {
        if (_versionHistory.TryGetValue(promptName, out var versions))
        {
            return versions.OrderByDescending(v => ParseVersion(v.Version)).FirstOrDefault();
        }

        return null;
    }

    /// <summary>
    /// Compares two prompt versions and returns the differences.
    /// </summary>
    public VersionComparison Compare(Prompt oldVersion, Prompt newVersion)
    {
        var comparison = new VersionComparison
        {
            OldVersion = oldVersion.Metadata.Version,
            NewVersion = newVersion.Metadata.Version,
            PromptName = oldVersion.Metadata.Name
        };

        // Content changes
        if (oldVersion.ContentHash != newVersion.ContentHash)
        {
            comparison.ContentChanged = true;
            comparison.Changes.Add("Prompt content modified");
        }

        // Metadata changes
        if (oldVersion.Metadata.Description != newVersion.Metadata.Description)
        {
            comparison.Changes.Add($"Description changed from '{oldVersion.Metadata.Description}' to '{newVersion.Metadata.Description}'");
        }

        // Model requirements changes
        CompareModelRequirements(oldVersion.Metadata.ModelRequirements, newVersion.Metadata.ModelRequirements, comparison);

        // Schema changes
        CompareSchemas(oldVersion.Metadata.InputSchema, newVersion.Metadata.InputSchema, comparison);

        comparison.IsBreakingChange = DetermineIfBreaking(comparison);

        return comparison;
    }

    private void CompareModelRequirements(
        ModelRequirements? oldReqs,
        ModelRequirements? newReqs,
        VersionComparison comparison)
    {
        if (oldReqs == null && newReqs == null) return;

        if (oldReqs == null || newReqs == null)
        {
            comparison.Changes.Add("Model requirements added or removed");
            return;
        }

        if (oldReqs.MinTokens != newReqs.MinTokens)
        {
            comparison.Changes.Add($"Min tokens changed from {oldReqs.MinTokens} to {newReqs.MinTokens}");
        }

        if (oldReqs.Temperature != newReqs.Temperature)
        {
            comparison.Changes.Add($"Temperature changed from {oldReqs.Temperature} to {newReqs.Temperature}");
        }
    }

    private void CompareSchemas(
        List<PromptParameter>? oldSchema,
        List<PromptParameter>? newSchema,
        VersionComparison comparison)
    {
        if (oldSchema == null && newSchema == null) return;

        var oldParams = oldSchema?.Select(p => p.Name).ToHashSet() ?? new HashSet<string>();
        var newParams = newSchema?.Select(p => p.Name).ToHashSet() ?? new HashSet<string>();

        // Removed parameters
        var removedParams = oldParams.Except(newParams).ToList();
        if (removedParams.Any())
        {
            comparison.Changes.Add($"Removed parameters: {string.Join(", ", removedParams)}");
            comparison.SchemaChanged = true;
        }

        // Added parameters
        var addedParams = newParams.Except(oldParams).ToList();
        if (addedParams.Any())
        {
            comparison.Changes.Add($"Added parameters: {string.Join(", ", addedParams)}");
            comparison.SchemaChanged = true;
        }

        // Changed parameter requirements
        if (oldSchema != null && newSchema != null)
        {
            foreach (var oldParam in oldSchema)
            {
                var newParam = newSchema.FirstOrDefault(p => p.Name == oldParam.Name);
                if (newParam != null && oldParam.Required != newParam.Required)
                {
                    comparison.Changes.Add(
                        $"Parameter '{oldParam.Name}' required status changed from {oldParam.Required} to {newParam.Required}");
                    comparison.SchemaChanged = true;
                }
            }
        }
    }

    private bool DetermineIfBreaking(VersionComparison comparison)
    {
        // Breaking changes include:
        // - Removed required parameters
        // - Changed parameter from optional to required
        // - Significant schema changes
        return comparison.Changes.Any(c =>
            c.Contains("Removed parameters") ||
            c.Contains("required status changed from False to True") ||
            c.Contains("Min tokens changed") && comparison.Changes.Any(ch => ch.Contains("increased")));
    }

    private static (int major, int minor, int patch) ParseVersion(string version)
    {
        var parts = version.Split('-')[0].Split('.'); // Remove pre-release tags
        if (parts.Length >= 3 &&
            int.TryParse(parts[0], out var major) &&
            int.TryParse(parts[1], out var minor) &&
            int.TryParse(parts[2], out var patch))
        {
            return (major, minor, patch);
        }

        return (0, 0, 0);
    }

    /// <summary>
    /// Suggests the next version based on changes.
    /// </summary>
    public string SuggestNextVersion(string currentVersion, VersionComparison comparison)
    {
        var (major, minor, patch) = ParseVersion(currentVersion);

        if (comparison.IsBreakingChange)
        {
            return $"{major + 1}.0.0";
        }

        if (comparison.SchemaChanged || comparison.Changes.Count > 3)
        {
            return $"{major}.{minor + 1}.0";
        }

        return $"{major}.{minor}.{patch + 1}";
    }
}

/// <summary>
/// Represents a specific version of a prompt.
/// </summary>
public class PromptVersion
{
    public required string Name { get; set; }
    public required string Version { get; set; }
    public required string ContentHash { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? FilePath { get; set; }
    public required PromptMetadata Metadata { get; set; }
}

/// <summary>
/// Comparison result between two prompt versions.
/// </summary>
public class VersionComparison
{
    public required string PromptName { get; set; }
    public required string OldVersion { get; set; }
    public required string NewVersion { get; set; }
    public List<string> Changes { get; set; } = new();
    public bool ContentChanged { get; set; }
    public bool SchemaChanged { get; set; }
    public bool IsBreakingChange { get; set; }

    public override string ToString()
    {
        return $"{PromptName}: {OldVersion} → {NewVersion} " +
               $"({Changes.Count} changes, Breaking: {IsBreakingChange})";
    }
}
