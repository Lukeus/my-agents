namespace Agents.Domain.BimClassification.Entities;

/// <summary>
/// Read-only view of a BIM element for classification analysis.
/// This is NOT the canonical BIM element - it's a snapshot for the agent.
/// </summary>
public sealed class BimElementView
{
    public long Id { get; init; }
    public string ExternalId { get; init; } = default!;
    public string ProjectId { get; init; } = default!;
    public string Category { get; init; } = default!;
    public string? Family { get; init; }
    public string? Type { get; init; }
    public string? Spec { get; init; }
    public string? LocationType { get; init; }  // Indoor/Outdoor/Roof/etc.
    public string? Material { get; init; }
    public decimal? LengthMm { get; init; }
    public decimal? WidthMm { get; init; }
    public decimal? HeightMm { get; init; }
    public decimal? DiameterMm { get; init; }
    public string? MetaJson { get; init; }  // Additional BIM properties as JSON
}
