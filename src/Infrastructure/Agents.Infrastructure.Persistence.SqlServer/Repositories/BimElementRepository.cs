using Agents.Domain.BimClassification.Entities;
using Agents.Domain.BimClassification.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Agents.Infrastructure.Persistence.SqlServer.Repositories;

/// <summary>
/// SQL Server implementation of BIM element repository.
/// Uses indexed views for efficient pattern aggregation.
/// </summary>
public class BimElementRepository : IBimElementRepository
{
    private readonly DbContext _context;
    private readonly ILogger<BimElementRepository> _logger;

    public BimElementRepository(
        DbContext context,
        ILogger<BimElementRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IReadOnlyList<BimElementView>> GetByIdsAsync(
        IEnumerable<long> elementIds,
        CancellationToken cancellationToken = default)
    {
        var ids = elementIds.ToList();
        
        // Query against indexed view for better performance
        var elements = await _context.Set<BimElementView>()
            .Where(e => ids.Contains(e.Id))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return elements;
    }

    public async Task<IReadOnlyList<BimPattern>> GetPatternsByElementIdsAsync(
        IEnumerable<long> elementIds,
        int sampleSize = 50,
        CancellationToken cancellationToken = default)
    {
        var ids = elementIds.ToList();

        // Step 1: Aggregate elements into patterns using SQL grouping
        var patternGroups = await _context.Set<BimElementView>()
            .Where(e => ids.Contains(e.Id))
            .GroupBy(e => new
            {
                e.Category,
                e.Family,
                e.Type,
                e.Material,
                e.LocationType
            })
            .Select(g => new
            {
                g.Key.Category,
                g.Key.Family,
                g.Key.Type,
                g.Key.Material,
                g.Key.LocationType,
                ElementCount = g.Count(),
                // Compute dimension statistics
                LengthMin = g.Min(e => e.LengthMm),
                LengthMax = g.Max(e => e.LengthMm),
                LengthAvg = g.Average(e => e.LengthMm),
                WidthMin = g.Min(e => e.WidthMm),
                WidthMax = g.Max(e => e.WidthMm),
                WidthAvg = g.Average(e => e.WidthMm),
                HeightMin = g.Min(e => e.HeightMm),
                HeightMax = g.Max(e => e.HeightMm),
                HeightAvg = g.Average(e => e.HeightMm),
                DiameterMin = g.Min(e => e.DiameterMm),
                DiameterMax = g.Max(e => e.DiameterMm),
                DiameterAvg = g.Average(e => e.DiameterMm),
                ElementIds = g.Select(e => e.Id).Take(sampleSize).ToList()
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Step 2: Fetch sample elements for each pattern
        var patterns = new List<BimPattern>();

        foreach (var group in patternGroups)
        {
            var sampleElements = await _context.Set<BimElementView>()
                .Where(e => group.ElementIds.Contains(e.Id))
                .Take(sampleSize)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var pattern = new BimPattern
            {
                PatternKey = $"{group.Category}_{group.Family}_{group.Type}",
                Category = group.Category,
                Family = group.Family,
                Type = group.Type,
                Material = group.Material,
                LocationType = group.LocationType,
                ElementCount = group.ElementCount,
                SampleElements = sampleElements,
                DimensionStats = new DimensionStatistics
                {
                    LengthMin = group.LengthMin,
                    LengthMax = group.LengthMax,
                    LengthAvg = group.LengthAvg,
                    WidthMin = group.WidthMin,
                    WidthMax = group.WidthMax,
                    WidthAvg = group.WidthAvg,
                    HeightMin = group.HeightMin,
                    HeightMax = group.HeightMax,
                    HeightAvg = group.HeightAvg,
                    DiameterMin = group.DiameterMin,
                    DiameterMax = group.DiameterMax,
                    DiameterAvg = group.DiameterAvg
                }
            };

            patterns.Add(pattern);
        }

        _logger.LogInformation("Aggregated {ElementCount} elements into {PatternCount} patterns",
            ids.Count, patterns.Count);

        return patterns;
    }

    public async Task<IReadOnlyList<BimPattern>> GetAllPatternsAsync(
        int skip = 0,
        int take = 1000,
        CancellationToken cancellationToken = default)
    {
        // Use indexed view for efficient pattern listing
        var patternSummaries = await _context.Database
            .SqlQuery<PatternSummaryDto>($@"
                SELECT 
                    Category,
                    Family,
                    Type,
                    Material,
                    LocationType,
                    COUNT(*) as ElementCount
                FROM vw_BimElementPatterns
                GROUP BY Category, Family, Type, Material, LocationType
                ORDER BY ElementCount DESC
                OFFSET {skip} ROWS
                FETCH NEXT {take} ROWS ONLY
            ")
            .ToListAsync(cancellationToken);

        var patterns = new List<BimPattern>();

        foreach (var summary in patternSummaries)
        {
            // Fetch sample elements for this pattern
            var samples = await _context.Set<BimElementView>()
                .Where(e => e.Category == summary.Category
                         && e.Family == summary.Family
                         && e.Type == summary.Type
                         && e.Material == summary.Material
                         && e.LocationType == summary.LocationType)
                .Take(50)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            patterns.Add(new BimPattern
            {
                PatternKey = $"{summary.Category}_{summary.Family}_{summary.Type}",
                Category = summary.Category,
                Family = summary.Family,
                Type = summary.Type,
                Material = summary.Material,
                LocationType = summary.LocationType,
                ElementCount = summary.ElementCount,
                SampleElements = samples
            });
        }

        return patterns;
    }

    public async Task<long> GetPatternCountAsync(CancellationToken cancellationToken = default)
    {
        var count = await _context.Database
            .SqlQuery<long>($@"
                SELECT COUNT(*)
                FROM (
                    SELECT DISTINCT Category, Family, Type, Material, LocationType
                    FROM vw_BimElementPatterns
                ) AS DistinctPatterns
            ")
            .FirstAsync(cancellationToken);

        return count;
    }
}

/// <summary>
/// DTO for pattern summary queries.
/// </summary>
internal class PatternSummaryDto
{
    public string Category { get; set; } = default!;
    public string? Family { get; set; }
    public string? Type { get; set; }
    public string? Material { get; set; }
    public string? LocationType { get; set; }
    public long ElementCount { get; set; }
}
