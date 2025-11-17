using Agents.Application.Core;
using Agents.Domain.BimClassification.Entities;
using Agents.Domain.BimClassification.Interfaces;
using Microsoft.Extensions.Logging;

namespace Agents.Application.BimClassification.Services;

/// <summary>
/// Application service for BIM classification using pattern aggregation and caching.
/// Orchestrates repository, cache, and agent interactions.
/// </summary>
public class BimClassificationService
{
    private readonly IBimElementRepository _elementRepository;
    private readonly IClassificationCacheRepository _cacheRepository;
    private readonly BimClassificationAgent _agent;
    private readonly ILogger<BimClassificationService> _logger;

    public BimClassificationService(
        IBimElementRepository elementRepository,
        IClassificationCacheRepository cacheRepository,
        BimClassificationAgent agent,
        ILogger<BimClassificationService> logger)
    {
        _elementRepository = elementRepository;
        _cacheRepository = cacheRepository;
        _agent = agent;
        _logger = logger;
    }

    /// <summary>
    /// Classifies a batch of BIM elements using pattern aggregation and multi-level caching.
    /// </summary>
    public async Task<BatchClassificationResult> ClassifyBatchAsync(
        IEnumerable<long> elementIds,
        AgentContext context,
        CancellationToken cancellationToken = default)
    {
        var elementIdList = elementIds.ToList();
        _logger.LogInformation("Starting batch classification for {Count} elements", elementIdList.Count);

        // Step 1: Aggregate elements into patterns
        var patterns = await _elementRepository.GetPatternsByElementIdsAsync(
            elementIdList,
            sampleSize: 50,
            cancellationToken);

        _logger.LogInformation("Aggregated {ElementCount} elements into {PatternCount} patterns",
            elementIdList.Count, patterns.Count);

        // Step 2: Check cache for existing classifications
        var patternHashes = patterns.Select(p => p.GetPatternHash()).ToList();
        var cachedSuggestions = await _cacheRepository.GetManyByPatternHashesAsync(
            patternHashes,
            cancellationToken);

        _logger.LogInformation("Cache hit for {CacheHits}/{TotalPatterns} patterns",
            cachedSuggestions.Count, patterns.Count);

        // Step 3: Classify uncached patterns
        var uncachedPatterns = patterns
            .Where(p => !cachedSuggestions.ContainsKey(p.GetPatternHash()))
            .ToList();

        var newSuggestions = new List<BimClassificationSuggestion>();

        foreach (var pattern in uncachedPatterns)
        {
            try
            {
                var suggestion = await ClassifyPatternAsync(pattern, context, cancellationToken);
                if (suggestion != null)
                {
                    newSuggestions.Add(suggestion);

                    // Cache the new suggestion
                    await _cacheRepository.SetByPatternHashAsync(
                        pattern.GetPatternHash(),
                        suggestion,
                        expiration: TimeSpan.FromHours(24),
                        cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error classifying pattern {PatternKey}", pattern.PatternKey);
            }
        }

        _logger.LogInformation("Classified {NewCount} new patterns", newSuggestions.Count);

        // Step 4: Combine cached and new suggestions
        var allSuggestions = cachedSuggestions.Values
            .Concat(newSuggestions)
            .ToList();

        return new BatchClassificationResult
        {
            TotalElements = elementIdList.Count,
            TotalPatterns = patterns.Count,
            CachedPatterns = cachedSuggestions.Count,
            NewlyClassifiedPatterns = newSuggestions.Count,
            Suggestions = allSuggestions,
            PatternMapping = patterns.ToDictionary(
                p => p.GetPatternHash(),
                p => p.SampleElements.Select(e => e.Id).ToList())
        };
    }

    /// <summary>
    /// Classifies a single pattern using the BIM classification agent.
    /// </summary>
    private async Task<BimClassificationSuggestion?> ClassifyPatternAsync(
        BimPattern pattern,
        AgentContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Convert pattern to prompt-optimized format
        var patternPrompt = ConvertPatternToPrompt(pattern);

        var request = new Requests.ClassifyBimPatternRequest
        {
            PatternKey = pattern.PatternKey,
            PatternHash = pattern.GetPatternHash(),
            ElementCount = pattern.ElementCount,
            PatternJson = System.Text.Json.JsonSerializer.Serialize(patternPrompt)
        };

        var input = System.Text.Json.JsonSerializer.Serialize(request);
        var result = await _agent.ExecuteAsync(input, context);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to classify pattern {PatternKey}: {Error}",
                pattern.PatternKey, result.ErrorMessage);
            return null;
        }

        // Extract suggestion from result
        if (result is AgentResult<Responses.ClassifyBimElementResponse> typedResult)
        {
            var response = typedResult.Data;
            if (response?.NormalizedSuggestionJson != null)
            {
                return System.Text.Json.JsonSerializer.Deserialize<BimClassificationSuggestion>(
                    response.NormalizedSuggestionJson);
            }
        }

        return null;
    }

    /// <summary>
    /// Converts pattern to token-efficient prompt format.
    /// </summary>
    private static object ConvertPatternToPrompt(BimPattern pattern)
    {
        return new
        {
            patternKey = pattern.PatternKey,
            category = pattern.Category,
            family = pattern.Family,
            type = pattern.Type,
            material = pattern.Material,
            locationType = pattern.LocationType,
            elementCount = pattern.ElementCount,
            dimensions = pattern.DimensionStats != null ? new
            {
                length = new { min = pattern.DimensionStats.LengthMin, max = pattern.DimensionStats.LengthMax, avg = pattern.DimensionStats.LengthAvg },
                width = new { min = pattern.DimensionStats.WidthMin, max = pattern.DimensionStats.WidthMax, avg = pattern.DimensionStats.WidthAvg },
                height = new { min = pattern.DimensionStats.HeightMin, max = pattern.DimensionStats.HeightMax, avg = pattern.DimensionStats.HeightAvg },
                diameter = new { min = pattern.DimensionStats.DiameterMin, max = pattern.DimensionStats.DiameterMax, avg = pattern.DimensionStats.DiameterAvg }
            } : null,
            samples = pattern.SampleElements.Take(5).Select(e => new
            {
                id = e.Id,
                spec = e.Spec,
                metaJson = e.MetaJson
            })
        };
    }
}

/// <summary>
/// Result of batch classification operation.
/// </summary>
public sealed class BatchClassificationResult
{
    public int TotalElements { get; init; }
    public int TotalPatterns { get; init; }
    public int CachedPatterns { get; init; }
    public int NewlyClassifiedPatterns { get; init; }
    public IReadOnlyList<BimClassificationSuggestion> Suggestions { get; init; } = Array.Empty<BimClassificationSuggestion>();

    /// <summary>
    /// Maps pattern hash to list of element IDs belonging to that pattern.
    /// </summary>
    public Dictionary<string, List<long>> PatternMapping { get; init; } = new();
}
