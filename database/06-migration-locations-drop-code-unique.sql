-- Locations: code is no longer used; allow duplicate empty Code by dropping the unique index.
IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Locations_Code' AND object_id = OBJECT_ID(N'dbo.Locations'))
BEGIN
    DROP INDEX [IX_Locations_Code] ON dbo.Locations;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.default_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.Locations') AND name = N'DF_Locations_Code')
BEGIN
    ALTER TABLE dbo.Locations ADD CONSTRAINT [DF_Locations_Code] DEFAULT (N'') FOR [Code];
END
GO

UPDATE dbo.Locations SET Code = N'';
GO
