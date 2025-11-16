using System.Text;

namespace Agents.Domain.BimClassification.ValueObjects;

/// <summary>
/// Token-efficient representation of a BIM element for prompt generation.
/// </summary>
public sealed class BimElementPromptModel
{
    public long Id { get; init; }
    public string Category { get; init; } = default!;
    public string? Family { get; init; }
    public string? Type { get; init; }
    public string? Spec { get; init; }
    public string? LocationType { get; init; }
    public string? Material { get; init; }
    public decimal? LengthMm { get; init; }
    public decimal? WidthMm { get; init; }
    public decimal? HeightMm { get; init; }
    public decimal? DiameterMm { get; init; }
    public IReadOnlyDictionary<string, string> Meta { get; init; } =
        new Dictionary<string, string>();

    /// <summary>
    /// Formats to token-efficient prompt text.
    /// </summary>
    public string ToPromptString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"ElementId: {Id}");
        sb.AppendLine($"Category: {Category}");

        if (!string.IsNullOrWhiteSpace(Spec))
            sb.AppendLine($"Spec: {Spec}");
        if (!string.IsNullOrWhiteSpace(LocationType))
            sb.AppendLine($"LocationType: {LocationType}");
        if (!string.IsNullOrWhiteSpace(Material))
            sb.AppendLine($"Material: {Material}");
        if (!string.IsNullOrWhiteSpace(Family))
            sb.AppendLine($"Family: {Family}");
        if (!string.IsNullOrWhiteSpace(Type))
            sb.AppendLine($"Type: {Type}");

        if (LengthMm is not null || WidthMm is not null ||
            HeightMm is not null || DiameterMm is not null)
        {
            sb.AppendLine("Dimensions (mm):");
            if (LengthMm is not null) sb.AppendLine($"  Length: {LengthMm}");
            if (WidthMm is not null) sb.AppendLine($"  Width: {WidthMm}");
            if (HeightMm is not null) sb.AppendLine($"  Height: {HeightMm}");
            if (DiameterMm is not null) sb.AppendLine($"  Diameter: {DiameterMm}");
        }

        if (Meta.Count > 0)
        {
            sb.AppendLine("Metadata:");
            foreach (var (key, value) in Meta)
            {
                var trimmed = value.Length > 80 ? value[..80] + "..." : value;
                sb.AppendLine($"  {key}: {trimmed}");
            }
        }

        return sb.ToString();
    }
}
