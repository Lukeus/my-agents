namespace Agents.Application.BimClassification.Requests;

public sealed class ClassifyBimElementRequest
{
    public long BimElementId { get; init; }

    /// <summary>
    /// Optional: pre-fetched BIM element data as JSON
    /// </summary>
    public string? ElementJson { get; init; }

    /// <summary>
    /// Optional: existing classification JSON (if any)
    /// </summary>
    public string? ExistingClassificationJson { get; init; }
}
