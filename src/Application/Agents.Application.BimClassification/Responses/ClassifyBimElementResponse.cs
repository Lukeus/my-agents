namespace Agents.Application.BimClassification.Responses;

public sealed class ClassifyBimElementResponse
{
    public long SuggestionId { get; init; }
    public string RawModelOutput { get; init; } = default!;
    public string NormalizedSuggestionJson { get; init; } = default!;
}
