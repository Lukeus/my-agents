using Agents.Domain.BimClassification.Entities;

namespace Agents.Domain.BimClassification.Interfaces;

/// <summary>
/// Repository interface for querying BIM elements.
/// Implementations reside in Infrastructure layer.
/// </summary>
public interface IBimElementRepository
{
    /// <summary>
    /// Retrieves BIM elements by their IDs.
    /// </summary>
    Task<IReadOnlyList<BimElementView>> GetByIdsAsync(
        IEnumerable<long> elementIds, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Aggregates BIM elements into patterns for efficient processing.
    /// </summary>
    Task<IReadOnlyList<BimPattern>> GetPatternsByElementIdsAsync(
        IEnumerable<long> elementIds,
        int sampleSize = 50,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all distinct patterns in the dataset.
    /// Used for bulk pre-processing scenarios.
    /// </summary>
    Task<IReadOnlyList<BimPattern>> GetAllPatternsAsync(
        int skip = 0,
        int take = 1000,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Counts total number of distinct patterns.
    /// </summary>
    Task<long> GetPatternCountAsync(CancellationToken cancellationToken = default);
}
