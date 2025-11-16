-- Migration: Add indexed view for BIM element pattern aggregation
-- This view dramatically improves performance for pattern-based queries on 100M+ records
--
-- PREREQUISITES:
-- This migration requires the dbo.BimElements table to exist with the following schema:
--   - Id (bigint/int, primary key)
--   - ExternalId (nvarchar)
--   - ProjectId (uniqueidentifier/int)
--   - Category (nvarchar, NOT NULL)
--   - Family (nvarchar, nullable)
--   - Type (nvarchar, nullable)
--   - Material (nvarchar, nullable)
--   - LocationType (nvarchar, nullable)
--   - LengthMm, WidthMm, HeightMm, DiameterMm (decimal, nullable)
--   - Spec (nvarchar, nullable)
--   - MetaJson (nvarchar(max), nullable)
--
-- If your table has a different name or schema, adjust accordingly.

-- Verify prerequisite table exists
IF OBJECT_ID('dbo.BimElements', 'U') IS NULL
BEGIN
    RAISERROR('Table dbo.BimElements does not exist. Please create it before running this migration.', 16, 1);
    RETURN;
END
GO

-- Drop view if exists (for re-running migration)
IF OBJECT_ID('dbo.vw_BimElementPatterns', 'V') IS NOT NULL
    DROP VIEW dbo.vw_BimElementPatterns;
GO

-- Create indexed view for pattern aggregation
CREATE VIEW dbo.vw_BimElementPatterns
WITH SCHEMABINDING
AS
SELECT 
    Id,
    ExternalId,
    ProjectId,
    Category,
    ISNULL(Family, '') AS Family,
    ISNULL([Type], '') AS [Type],
    ISNULL(Material, '') AS Material,
    ISNULL(LocationType, '') AS LocationType,
    LengthMm,
    WidthMm,
    HeightMm,
    DiameterMm,
    Spec,
    MetaJson
FROM dbo.BimElements
WHERE 1 = 1;  -- Placeholder for additional filters
GO

-- Create clustered index on the view for materialization
-- This makes pattern queries nearly instant even on 100M records
CREATE UNIQUE CLUSTERED INDEX IX_BimElementPatterns_Clustered
ON dbo.vw_BimElementPatterns (Category, Family, [Type], Material, LocationType, Id);
GO

-- Create additional non-clustered indexes for common query patterns
CREATE NONCLUSTERED INDEX IX_BimElementPatterns_Category
ON dbo.vw_BimElementPatterns (Category)
INCLUDE (Family, [Type], Material, LocationType);
GO

CREATE NONCLUSTERED INDEX IX_BimElementPatterns_Id
ON dbo.vw_BimElementPatterns (Id);
GO

-- Statistics for query optimization
CREATE STATISTICS STAT_PatternDistribution
ON dbo.vw_BimElementPatterns (Category, Family, [Type], Material, LocationType);
GO

-- Grant appropriate permissions
-- GRANT SELECT ON dbo.vw_BimElementPatterns TO [YourAppRole];
-- GO

PRINT 'Created indexed view: vw_BimElementPatterns with clustered index';
PRINT 'This view will dramatically improve pattern aggregation performance';
GO
