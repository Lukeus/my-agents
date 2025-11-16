namespace Agents.Application.BimClassification.Requests;

/// <summary>
/// Request to classify a BIM element pattern (not individual elements).
/// Used for efficient batch processing of similar elements.
/// </summary>
public sealed class ClassifyBimPatternRequest
{
    public string PatternKey { get; init; } = default!;
    public string PatternHash { get; init; } = default!;
    public long ElementCount { get; init; }

    /// <summary>
    /// JSON representation of the pattern with statistical summaries.
    /// </summary>
    public string PatternJson { get; init; } = default!;

    /// <summary>
    /// Optional: existing classification JSON for this pattern (if any).
    /// </summary>
    public string? ExistingClassificationJson { get; init; }
}
