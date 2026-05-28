-- Virtual "documents": display name + external URL (no file on disk).
IF COL_LENGTH(N'dbo.Documents', N'IsLink') IS NULL
BEGIN
    ALTER TABLE dbo.Documents ADD [IsLink] BIT NOT NULL CONSTRAINT [DF_Documents_IsLink] DEFAULT 0;
END
GO

IF COL_LENGTH(N'dbo.Documents', N'ExternalUrl') IS NULL
BEGIN
    ALTER TABLE dbo.Documents ADD [ExternalUrl] NVARCHAR(2000) NULL;
END
GO
