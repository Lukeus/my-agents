namespace Agents.Application.BimClassification.Requests;

/// <summary>
/// Request to classify multiple BIM elements in a batch.
/// Elements will be automatically aggregated into patterns.
/// </summary>
public sealed class BatchClassifyRequest
{
    /// <summary>
    /// List of BIM element IDs to classify.
    /// </summary>
    public List<long> ElementIds { get; init; } = new();
    
    /// <summary>
    /// Optional: Project ID for filtering (if multi-tenant).
    /// </summary>
    public string? ProjectId { get; init; }
    
    /// <summary>
    /// Whether to force re-classification even if cached.
    /// </summary>
    public bool ForceRefresh { get; init; } = false;
}
