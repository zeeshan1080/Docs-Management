-- Optional migration for existing databases (skip if 01-schema.sql was applied fresh).
IF COL_LENGTH(N'dbo.FolderShares', N'AccessLevel') IS NULL
BEGIN
    ALTER TABLE dbo.FolderShares ADD AccessLevel TINYINT NOT NULL CONSTRAINT [DF_FolderShares_AccessLevel] DEFAULT 2;
    CREATE INDEX [IX_FolderShares_AccessLevel] ON dbo.FolderShares ([AccessLevel]);
END
GO
