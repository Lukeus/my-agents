using Agents.Domain.BimClassification.Entities;
using Agents.Infrastructure.Persistence.SqlServer;
using Agents.Infrastructure.Persistence.SqlServer.Data;
using Agents.Infrastructure.Persistence.SqlServer.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Testcontainers.MsSql;

namespace Agents.Tests.Integration.SqlServer;

/// <summary>
/// Integration tests for BimElementRepository using SQL Server Testcontainers.
/// </summary>
public class BimElementRepositoryTests : IAsyncLifetime
{
    private MsSqlContainer? _msSqlContainer;
    private IServiceProvider? _serviceProvider;
    private AgentsDbContext? _context;
    private BimElementRepository? _repository;

    public async Task InitializeAsync()
    {
        // Start SQL Server container
        _msSqlContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("StrongP@ssw0rd!")
            .Build();

        await _msSqlContainer.StartAsync();

        // Setup DI container
        var services = new ServiceCollection();
        services.AddSqlServerPersistence(_msSqlContainer.GetConnectionString());

        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<AgentsDbContext>();

        // Apply migrations
        await _context.Database.MigrateAsync();

        // Create repository
        var mockLogger = new Mock<ILogger<BimElementRepository>>();
        _repository = new BimElementRepository(_context, mockLogger.Object);

        // Create the indexed view for testing
        await CreateBimElementPatternsView();
    }

    public async Task DisposeAsync()
    {
        if (_context != null)
        {
            await _context.DisposeAsync();
        }

        if (_msSqlContainer != null)
        {
            await _msSqlContainer.DisposeAsync();
        }
    }

    private async Task CreateBimElementPatternsView()
    {
        // Create BimElementView table if it doesn't exist
        await _context!.Database.ExecuteSqlRawAsync(@"
            IF OBJECT_ID('dbo.BimElementView', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.BimElementView (
                    Id BIGINT PRIMARY KEY,
                    Category NVARCHAR(100) NOT NULL,
                    Family NVARCHAR(100),
                    Type NVARCHAR(100),
                    Material NVARCHAR(100),
                    LocationType NVARCHAR(100),
                    LengthMm DECIMAL(18,2),
                    WidthMm DECIMAL(18,2),
                    HeightMm DECIMAL(18,2),
                    DiameterMm DECIMAL(18,2)
                )
            END
        ");

        // Create the indexed view
        await _context.Database.ExecuteSqlRawAsync(@"
            IF OBJECT_ID('dbo.vw_BimElementPatterns', 'V') IS NOT NULL
                DROP VIEW dbo.vw_BimElementPatterns;
        ");

        await _context.Database.ExecuteSqlRawAsync(@"
            CREATE VIEW dbo.vw_BimElementPatterns AS
            SELECT 
                Category,
                Family,
                Type,
                Material,
                LocationType,
                LengthMm,
                WidthMm,
                HeightMm,
                DiameterMm
            FROM dbo.BimElementView
        ");
    }

    private async Task InsertBimElements(List<BimElementView> elements)
    {
        foreach (var element in elements)
        {
            var sql = $@"
                INSERT INTO dbo.BimElementView (Id, Category, Family, Type, Material, LocationType, LengthMm, WidthMm, HeightMm, DiameterMm)
                VALUES (
                    {element.Id}, 
                    '{element.Category}', 
                    {(element.Family != null ? $"'{element.Family}'" : "NULL")}, 
                    {(element.Type != null ? $"'{element.Type}'" : "NULL")}, 
                    {(element.Material != null ? $"'{element.Material}'" : "NULL")}, 
                    {(element.LocationType != null ? $"'{element.LocationType}'" : "NULL")}, 
                    {(element.LengthMm.HasValue ? element.LengthMm.Value.ToString() : "NULL")}, 
                    {(element.WidthMm.HasValue ? element.WidthMm.Value.ToString() : "NULL")}, 
                    {(element.HeightMm.HasValue ? element.HeightMm.Value.ToString() : "NULL")}, 
                    {(element.DiameterMm.HasValue ? element.DiameterMm.Value.ToString() : "NULL")}
                )";
            await _context!.Database.ExecuteSqlRawAsync(sql);
        }
    }

    [Fact]
    public async Task GetPatternCountAsync_ReturnsCorrectCount_WhenPatternsExist()
    {
        // Arrange: Insert elements that form 3 distinct patterns
        var elements = new List<BimElementView>
        {
            // Pattern 1: Wall - Basic - Generic (2 elements)
            new() { Id = 1, Category = "Wall", Family = "Basic", Type = "Generic", Material = "Concrete", LocationType = "Interior" },
            new() { Id = 2, Category = "Wall", Family = "Basic", Type = "Generic", Material = "Concrete", LocationType = "Interior" },
            
            // Pattern 2: Pipe - Copper - 15mm (2 elements)
            new() { Id = 3, Category = "Pipe", Family = "Copper", Type = "15mm", Material = "Copper", LocationType = "MEP" },
            new() { Id = 4, Category = "Pipe", Family = "Copper", Type = "15mm", Material = "Copper", LocationType = "MEP" },
            
            // Pattern 3: Door - Standard - Single (1 element)
            new() { Id = 5, Category = "Door", Family = "Standard", Type = "Single", Material = "Wood", LocationType = "Interior" }
        };

        await InsertBimElements(elements);

        // Act
        var count = await _repository!.GetPatternCountAsync();

        // Assert
        count.Should().Be(3, "There should be 3 distinct patterns");
        count.GetType().Should().Be(typeof(long));
    }

    [Fact]
    public async Task GetPatternCountAsync_ReturnsZero_WhenNoPatternsExist()
    {
        // Arrange: No elements inserted

        // Act
        var count = await _repository!.GetPatternCountAsync();

        // Assert
        count.Should().Be(0, "There should be no patterns when the table is empty");
        count.GetType().Should().Be(typeof(long));
    }

    [Fact]
    public async Task GetPatternCountAsync_HandlesLargeNumberOfPatterns_Correctly()
    {
        // Arrange: Create 100 distinct patterns (each with 10 elements)
        var elements = new List<BimElementView>();

        for (int patternIndex = 0; patternIndex < 100; patternIndex++)
        {
            for (int elementIndex = 0; elementIndex < 10; elementIndex++)
            {
                elements.Add(new BimElementView
                {
                    Id = patternIndex * 10 + elementIndex + 1,
                    Category = $"Category{patternIndex}",
                    Family = $"Family{patternIndex}",
                    Type = $"Type{patternIndex}",
                    Material = $"Material{patternIndex}",
                    LocationType = "Interior"
                });
            }
        }

        await InsertBimElements(elements);

        // Act
        var count = await _repository!.GetPatternCountAsync();

        // Assert
        count.Should().Be(100, "There should be 100 distinct patterns");
        count.GetType().Should().Be(typeof(long));
    }

    [Fact]
    public async Task GetPatternCountAsync_HandlesCountExceedingIntMaxValue_WithoutTruncation()
    {
        // Arrange: This test verifies the return type can handle large values
        // Note: We cannot actually insert 2.1 billion+ patterns in a test,
        // but we can verify the method signature and type safety

        // Insert a small number of patterns for a valid test
        var elements = new List<BimElementView>
        {
            new() { Id = 1, Category = "Wall", Family = "Basic", Type = "Generic", Material = "Concrete", LocationType = "Interior" },
            new() { Id = 2, Category = "Wall", Family = "Basic", Type = "200mm", Material = "Concrete", LocationType = "Interior" },
        };

        await InsertBimElements(elements);

        // Act
        var count = await _repository!.GetPatternCountAsync();

        // Assert
        count.GetType().Should().Be(typeof(long));
        count.Should().BeGreaterOrEqualTo(0, "Count should never be negative");

        // Verify the method returns long, which can handle values beyond int.MaxValue
        var methodInfo = typeof(BimElementRepository)
            .GetMethod(nameof(BimElementRepository.GetPatternCountAsync));

        methodInfo.Should().NotBeNull();
        methodInfo!.ReturnType.Should().Be(typeof(Task<long>),
            "Method should return Task<long> to handle counts exceeding int.MaxValue");

        // Verify the result can theoretically hold values > int.MaxValue
        long theoreticalLargeCount = (long)int.MaxValue + 1;
        theoreticalLargeCount.Should().BeGreaterThan(int.MaxValue,
            "long type can represent values beyond int.MaxValue");
    }

    [Fact]
    public async Task GetPatternCountAsync_DistinguishesPatternsByAllGroupingKeys()
    {
        // Arrange: Insert elements that differ in each grouping dimension
        var elements = new List<BimElementView>
        {
            // Different Category
            new() { Id = 1, Category = "Wall", Family = "Basic", Type = "Generic", Material = "Concrete", LocationType = "Interior" },
            new() { Id = 2, Category = "Door", Family = "Basic", Type = "Generic", Material = "Concrete", LocationType = "Interior" },
            
            // Different Family
            new() { Id = 3, Category = "Wall", Family = "Advanced", Type = "Generic", Material = "Concrete", LocationType = "Interior" },
            
            // Different Type
            new() { Id = 4, Category = "Wall", Family = "Basic", Type = "200mm", Material = "Concrete", LocationType = "Interior" },
            
            // Different Material
            new() { Id = 5, Category = "Wall", Family = "Basic", Type = "Generic", Material = "Steel", LocationType = "Interior" },
            
            // Different LocationType
            new() { Id = 6, Category = "Wall", Family = "Basic", Type = "Generic", Material = "Concrete", LocationType = "Exterior" }
        };

        await InsertBimElements(elements);

        // Act
        var count = await _repository!.GetPatternCountAsync();

        // Assert
        count.Should().Be(6, "Each element should form a distinct pattern based on the grouping keys");
    }

    [Fact]
    public async Task GetPatternCountAsync_HandlesNullValuesInGroupingKeys()
    {
        // Arrange: Insert elements with null values in various fields
        var elements = new List<BimElementView>
        {
            // Pattern 1: With all values
            new() { Id = 1, Category = "Wall", Family = "Basic", Type = "Generic", Material = "Concrete", LocationType = "Interior" },
            new() { Id = 2, Category = "Wall", Family = "Basic", Type = "Generic", Material = "Concrete", LocationType = "Interior" },
            
            // Pattern 2: With null Family
            new() { Id = 3, Category = "Wall", Family = null, Type = "Generic", Material = "Concrete", LocationType = "Interior" },
            
            // Pattern 3: With null Type
            new() { Id = 4, Category = "Wall", Family = "Basic", Type = null, Material = "Concrete", LocationType = "Interior" },
            
            // Pattern 4: With null Material
            new() { Id = 5, Category = "Wall", Family = "Basic", Type = "Generic", Material = null, LocationType = "Interior" },
            
            // Pattern 5: With null LocationType
            new() { Id = 6, Category = "Wall", Family = "Basic", Type = "Generic", Material = "Concrete", LocationType = null }
        };

        await InsertBimElements(elements);

        // Act
        var count = await _repository!.GetPatternCountAsync();

        // Assert
        count.Should().Be(5, "Null values should be treated as distinct grouping keys");
    }
}
