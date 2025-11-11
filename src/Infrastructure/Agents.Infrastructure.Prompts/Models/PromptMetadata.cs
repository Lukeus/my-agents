namespace Agents.Infrastructure.Prompts.Models;

/// <summary>
/// Metadata for a prompt file including versioning and model requirements.
/// </summary>
public class PromptMetadata
{
    /// <summary>
    /// Unique name of the prompt.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Semantic version of the prompt (e.g., "1.0.0").
    /// </summary>
    public required string Version { get; set; }

    /// <summary>
    /// Human-readable description of the prompt's purpose.
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Author of the prompt.
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// Date the prompt was created.
    /// </summary>
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// Date the prompt was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Tags for categorization and search.
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Model requirements (token limits, temperature, etc.).
    /// </summary>
    public ModelRequirements? ModelRequirements { get; set; }

    /// <summary>
    /// Input schema definition.
    /// </summary>
    public List<PromptParameter>? InputSchema { get; set; }

    /// <summary>
    /// Output schema definition.
    /// </summary>
    public PromptOutputSchema? OutputSchema { get; set; }

    /// <summary>
    /// Whether this prompt is deprecated.
    /// </summary>
    public bool Deprecated { get; set; } = false;

    /// <summary>
    /// Replacement prompt name if deprecated.
    /// </summary>
    public string? ReplacedBy { get; set; }
}

/// <summary>
/// Model requirements for executing the prompt.
/// </summary>
public class ModelRequirements
{
    /// <summary>
    /// Minimum context window size (tokens).
    /// </summary>
    public int MinTokens { get; set; } = 4096;

    /// <summary>
    /// Recommended temperature setting (0.0 - 2.0).
    /// </summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// Top P sampling parameter.
    /// </summary>
    public double? TopP { get; set; }

    /// <summary>
    /// Maximum tokens to generate.
    /// </summary>
    public int? MaxTokens { get; set; }

    /// <summary>
    /// Presence penalty (-2.0 to 2.0).
    /// </summary>
    public double? PresencePenalty { get; set; }

    /// <summary>
    /// Frequency penalty (-2.0 to 2.0).
    /// </summary>
    public double? FrequencyPenalty { get; set; }

    /// <summary>
    /// Stop sequences.
    /// </summary>
    public List<string>? StopSequences { get; set; }
}
