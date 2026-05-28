-- Add IsDefault to Folders (system folders that must not be deleted).
IF COL_LENGTH(N'dbo.Folders', N'IsDefault') IS NULL
BEGIN
    ALTER TABLE dbo.Folders ADD [IsDefault] BIT NOT NULL CONSTRAINT [DF_Folders_IsDefault] DEFAULT 0;
END
GO
