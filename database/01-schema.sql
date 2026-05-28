-- Document Management - SQL Server schema (DB-first source of truth)
-- Standard audit: CreatedOn, CreatedBy, LastModifiedOn, LastModifiedBy, RecordStatusLIID (default 1).

IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NOT NULL DROP TABLE [__EFMigrationsHistory];
GO

IF OBJECT_ID(N'AuditLogs') IS NOT NULL DROP TABLE AuditLogs;
IF OBJECT_ID(N'EmployeeRegistrationRequests') IS NOT NULL DROP TABLE EmployeeRegistrationRequests;
IF OBJECT_ID(N'FolderShares') IS NOT NULL DROP TABLE FolderShares;
IF OBJECT_ID(N'DocumentShares') IS NOT NULL DROP TABLE DocumentShares;
IF OBJECT_ID(N'Documents') IS NOT NULL DROP TABLE Documents;
IF OBJECT_ID(N'Folders') IS NOT NULL DROP TABLE Folders;
IF OBJECT_ID(N'UserLocations') IS NOT NULL DROP TABLE UserLocations;
IF OBJECT_ID(N'Locations') IS NOT NULL DROP TABLE Locations;
IF OBJECT_ID(N'AspNetUserRoles') IS NOT NULL DROP TABLE AspNetUserRoles;
IF OBJECT_ID(N'AspNetUserClaims') IS NOT NULL DROP TABLE AspNetUserClaims;
IF OBJECT_ID(N'AspNetUserLogins') IS NOT NULL DROP TABLE AspNetUserLogins;
IF OBJECT_ID(N'AspNetUserTokens') IS NOT NULL DROP TABLE AspNetUserTokens;
IF OBJECT_ID(N'AspNetRoleClaims') IS NOT NULL DROP TABLE AspNetRoleClaims;
IF OBJECT_ID(N'AspNetUsers') IS NOT NULL DROP TABLE AspNetUsers;
IF OBJECT_ID(N'AspNetRoles') IS NOT NULL DROP TABLE AspNetRoles;
GO

CREATE TABLE [dbo].[AspNetUsers] (
    [Id] NVARCHAR(450) NOT NULL,
    [UserName] NVARCHAR(256) NULL,
    [NormalizedUserName] NVARCHAR(256) NULL,
    [Email] NVARCHAR(256) NULL,
    [NormalizedEmail] NVARCHAR(256) NULL,
    [EmailConfirmed] BIT NOT NULL DEFAULT 0,
    [PasswordHash] NVARCHAR(MAX) NULL,
    [SecurityStamp] NVARCHAR(MAX) NULL,
    [ConcurrencyStamp] NVARCHAR(MAX) NULL,
    [PhoneNumber] NVARCHAR(MAX) NULL,
    [PhoneNumberConfirmed] BIT NOT NULL DEFAULT 0,
    [TwoFactorEnabled] BIT NOT NULL DEFAULT 0,
    [LockoutEnd] DATETIMEOFFSET(7) NULL,
    [LockoutEnabled] BIT NOT NULL DEFAULT 1,
    [AccessFailedCount] INT NOT NULL DEFAULT 0,
    [FirstName] NVARCHAR(100) NOT NULL DEFAULT '',
    [LastName] NVARCHAR(100) NOT NULL DEFAULT '',
    [ApprovalStatus] TINYINT NOT NULL DEFAULT 0,
    [CreatedOn] DATETIME2 NOT NULL CONSTRAINT [DF_AspNetUsers_CreatedOn] DEFAULT SYSUTCDATETIME(),
    [CreatedBy] NVARCHAR(450) NULL,
    [LastModifiedOn] DATETIME2 NULL,
    [LastModifiedBy] NVARCHAR(450) NULL,
    [RecordStatusLIID] INT NOT NULL CONSTRAINT [DF_AspNetUsers_RecordStatusLIID] DEFAULT 1,
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
);
CREATE INDEX [IX_AspNetUsers_NormalizedEmail] ON [dbo].[AspNetUsers] ([NormalizedEmail]);
CREATE INDEX [IX_AspNetUsers_NormalizedUserName] ON [dbo].[AspNetUsers] ([NormalizedUserName]);
GO

CREATE TABLE [dbo].[AspNetRoles] (
    [Id] NVARCHAR(450) NOT NULL,
    [Name] NVARCHAR(256) NULL,
    [NormalizedName] NVARCHAR(256) NULL,
    [ConcurrencyStamp] NVARCHAR(MAX) NULL,
    [CreatedOn] DATETIME2 NOT NULL CONSTRAINT [DF_AspNetRoles_CreatedOn] DEFAULT SYSUTCDATETIME(),
    [CreatedBy] NVARCHAR(450) NULL,
    [LastModifiedOn] DATETIME2 NULL,
    [LastModifiedBy] NVARCHAR(450) NULL,
    [RecordStatusLIID] INT NOT NULL CONSTRAINT [DF_AspNetRoles_RecordStatusLIID] DEFAULT 1,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
);
CREATE UNIQUE INDEX [RoleNameIndex] ON [dbo].[AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;
GO

CREATE TABLE [dbo].[AspNetUserRoles] (
    [UserId] NVARCHAR(450) NOT NULL,
    [RoleId] NVARCHAR(450) NOT NULL,
    [CreatedOn] DATETIME2 NOT NULL CONSTRAINT [DF_AspNetUserRoles_CreatedOn] DEFAULT SYSUTCDATETIME(),
    [CreatedBy] NVARCHAR(450) NULL,
    [LastModifiedOn] DATETIME2 NULL,
    [LastModifiedBy] NVARCHAR(450) NULL,
    [RecordStatusLIID] INT NOT NULL CONSTRAINT [DF_AspNetUserRoles_RecordStatusLIID] DEFAULT 1,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles]([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [dbo].[AspNetUserClaims] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [UserId] NVARCHAR(450) NOT NULL,
    [ClaimType] NVARCHAR(MAX) NULL,
    [ClaimValue] NVARCHAR(MAX) NULL,
    [CreatedOn] DATETIME2 NOT NULL CONSTRAINT [DF_AspNetUserClaims_CreatedOn] DEFAULT SYSUTCDATETIME(),
    [CreatedBy] NVARCHAR(450) NULL,
    [LastModifiedOn] DATETIME2 NULL,
    [LastModifiedBy] NVARCHAR(450) NULL,
    [RecordStatusLIID] INT NOT NULL CONSTRAINT [DF_AspNetUserClaims_RecordStatusLIID] DEFAULT 1,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [dbo].[AspNetUserLogins] (
    [LoginProvider] NVARCHAR(450) NOT NULL,
    [ProviderKey] NVARCHAR(450) NOT NULL,
    [ProviderDisplayName] NVARCHAR(MAX) NULL,
    [UserId] NVARCHAR(450) NOT NULL,
    [CreatedOn] DATETIME2 NOT NULL CONSTRAINT [DF_AspNetUserLogins_CreatedOn] DEFAULT SYSUTCDATETIME(),
    [CreatedBy] NVARCHAR(450) NULL,
    [LastModifiedOn] DATETIME2 NULL,
    [LastModifiedBy] NVARCHAR(450) NULL,
    [RecordStatusLIID] INT NOT NULL CONSTRAINT [DF_AspNetUserLogins_RecordStatusLIID] DEFAULT 1,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [dbo].[AspNetUserTokens] (
    [UserId] NVARCHAR(450) NOT NULL,
    [LoginProvider] NVARCHAR(450) NOT NULL,
    [Name] NVARCHAR(450) NOT NULL,
    [Value] NVARCHAR(MAX) NULL,
    [CreatedOn] DATETIME2 NOT NULL CONSTRAINT [DF_AspNetUserTokens_CreatedOn] DEFAULT SYSUTCDATETIME(),
    [CreatedBy] NVARCHAR(450) NULL,
    [LastModifiedOn] DATETIME2 NULL,
    [LastModifiedBy] NVARCHAR(450) NULL,
    [RecordStatusLIID] INT NOT NULL CONSTRAINT [DF_AspNetUserTokens_RecordStatusLIID] DEFAULT 1,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [dbo].[AspNetRoleClaims] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [RoleId] NVARCHAR(450) NOT NULL,
    [ClaimType] NVARCHAR(MAX) NULL,
    [ClaimValue] NVARCHAR(MAX) NULL,
    [CreatedOn] DATETIME2 NOT NULL CONSTRAINT [DF_AspNetRoleClaims_CreatedOn] DEFAULT SYSUTCDATETIME(),
    [CreatedBy] NVARCHAR(450) NULL,
    [LastModifiedOn] DATETIME2 NULL,
    [LastModifiedBy] NVARCHAR(450) NULL,
    [RecordStatusLIID] INT NOT NULL CONSTRAINT [DF_AspNetRoleClaims_RecordStatusLIID] DEFAULT 1,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles]([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [dbo].[Locations] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [Code] NVARCHAR(50) NOT NULL CONSTRAINT [DF_Locations_Code] DEFAULT (N''),
    [CreatedOn] DATETIME2 NOT NULL CONSTRAINT [DF_Locations_CreatedOn] DEFAULT SYSUTCDATETIME(),
    [CreatedBy] NVARCHAR(450) NULL,
    [LastModifiedOn] DATETIME2 NULL,
    [LastModifiedBy] NVARCHAR(450) NULL,
    [RecordStatusLIID] INT NOT NULL CONSTRAINT [DF_Locations_RecordStatusLIID] DEFAULT 1,
    CONSTRAINT [PK_Locations] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [dbo].[UserLocations] (
    [UserId] NVARCHAR(450) NOT NULL,
    [LocationId] INT NOT NULL,
    [IsPrimary] BIT NOT NULL DEFAULT 1,
    [CreatedOn] DATETIME2 NOT NULL CONSTRAINT [DF_UserLocations_CreatedOn] DEFAULT SYSUTCDATETIME(),
    [CreatedBy] NVARCHAR(450) NULL,
    [LastModifiedOn] DATETIME2 NULL,
    [LastModifiedBy] NVARCHAR(450) NULL,
    [RecordStatusLIID] INT NOT NULL CONSTRAINT [DF_UserLocations_RecordStatusLIID] DEFAULT 1,
    CONSTRAINT [PK_UserLocations] PRIMARY KEY ([UserId], [LocationId]),
    CONSTRAINT [FK_UserLocations_AspNetUsers] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserLocations_Locations] FOREIGN KEY ([LocationId]) REFERENCES [Locations]([Id]) ON DELETE CASCADE
);
CREATE INDEX [IX_UserLocations_LocationId] ON [dbo].[UserLocations] ([LocationId]);
GO

CREATE TABLE [dbo].[Folders] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [Name] NVARCHAR(500) NOT NULL,
    [ParentFolderId] INT NULL,
    [IsDefault] BIT NOT NULL CONSTRAINT [DF_Folders_IsDefault] DEFAULT 0,
    [CreatedOn] DATETIME2 NOT NULL CONSTRAINT [DF_Folders_CreatedOn] DEFAULT SYSUTCDATETIME(),
    [CreatedBy] NVARCHAR(450) NULL,
    [LastModifiedOn] DATETIME2 NULL,
    [LastModifiedBy] NVARCHAR(450) NULL,
    [RecordStatusLIID] INT NOT NULL CONSTRAINT [DF_Folders_RecordStatusLIID] DEFAULT 1,
    CONSTRAINT [PK_Folders] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Folders_Folders] FOREIGN KEY ([ParentFolderId]) REFERENCES [Folders]([Id]),
    CONSTRAINT [FK_Folders_CreatedByUser] FOREIGN KEY ([CreatedBy]) REFERENCES [AspNetUsers]([Id])
);
CREATE INDEX [IX_Folders_ParentFolderId] ON [dbo].[Folders] ([ParentFolderId]);
GO

CREATE TABLE [dbo].[Documents] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [FolderId] INT NOT NULL,
    [OriginalFileName] NVARCHAR(500) NOT NULL,
    [StoredFileName] NVARCHAR(500) NOT NULL,
    [ContentType] NVARCHAR(200) NOT NULL,
    [SizeBytes] BIGINT NOT NULL,
    [IsLink] BIT NOT NULL CONSTRAINT [DF_Documents_IsLink] DEFAULT 0,
    [ExternalUrl] NVARCHAR(2000) NULL,
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [CreatedOn] DATETIME2 NOT NULL CONSTRAINT [DF_Documents_CreatedOn] DEFAULT SYSUTCDATETIME(),
    [CreatedBy] NVARCHAR(450) NULL,
    [LastModifiedOn] DATETIME2 NULL,
    [LastModifiedBy] NVARCHAR(450) NULL,
    [RecordStatusLIID] INT NOT NULL CONSTRAINT [DF_Documents_RecordStatusLIID] DEFAULT 1,
    CONSTRAINT [PK_Documents] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Documents_Folders] FOREIGN KEY ([FolderId]) REFERENCES [Folders]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Documents_CreatedByUser] FOREIGN KEY ([CreatedBy]) REFERENCES [AspNetUsers]([Id])
);
CREATE INDEX [IX_Documents_FolderId] ON [dbo].[Documents] ([FolderId]);
GO

CREATE TABLE [dbo].[DocumentShares] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [DocumentId] INT NOT NULL,
    [ShareType] TINYINT NOT NULL,
    [RoleId] NVARCHAR(450) NULL,
    [LocationId] INT NULL,
    [UserId] NVARCHAR(450) NULL,
    [CreatedOn] DATETIME2 NOT NULL CONSTRAINT [DF_DocumentShares_CreatedOn] DEFAULT SYSUTCDATETIME(),
    [CreatedBy] NVARCHAR(450) NULL,
    [LastModifiedOn] DATETIME2 NULL,
    [LastModifiedBy] NVARCHAR(450) NULL,
    [RecordStatusLIID] INT NOT NULL CONSTRAINT [DF_DocumentShares_RecordStatusLIID] DEFAULT 1,
    CONSTRAINT [PK_DocumentShares] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DocumentShares_Documents] FOREIGN KEY ([DocumentId]) REFERENCES [Documents]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_DocumentShares_AspNetRoles] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles]([Id]),
    CONSTRAINT [FK_DocumentShares_Locations] FOREIGN KEY ([LocationId]) REFERENCES [Locations]([Id]),
    CONSTRAINT [FK_DocumentShares_User] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]),
    CONSTRAINT [FK_DocumentShares_CreatedByUser] FOREIGN KEY ([CreatedBy]) REFERENCES [AspNetUsers]([Id])
);
CREATE INDEX [IX_DocumentShares_DocumentId] ON [dbo].[DocumentShares] ([DocumentId]);
CREATE INDEX [IX_DocumentShares_RoleId] ON [dbo].[DocumentShares] ([RoleId]);
CREATE INDEX [IX_DocumentShares_LocationId] ON [dbo].[DocumentShares] ([LocationId]);
CREATE INDEX [IX_DocumentShares_UserId] ON [dbo].[DocumentShares] ([UserId]);
GO

CREATE TABLE [dbo].[FolderShares] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [FolderId] INT NOT NULL,
    [ShareType] TINYINT NOT NULL,
    [AccessLevel] TINYINT NOT NULL CONSTRAINT [DF_FolderShares_AccessLevel] DEFAULT 2,
    [RoleId] NVARCHAR(450) NULL,
    [LocationId] INT NULL,
    [UserId] NVARCHAR(450) NULL,
    [CreatedOn] DATETIME2 NOT NULL CONSTRAINT [DF_FolderShares_CreatedOn] DEFAULT SYSUTCDATETIME(),
    [CreatedBy] NVARCHAR(450) NULL,
    [LastModifiedOn] DATETIME2 NULL,
    [LastModifiedBy] NVARCHAR(450) NULL,
    [RecordStatusLIID] INT NOT NULL CONSTRAINT [DF_FolderShares_RecordStatusLIID] DEFAULT 1,
    CONSTRAINT [PK_FolderShares] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_FolderShares_Folders] FOREIGN KEY ([FolderId]) REFERENCES [Folders]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_FolderShares_AspNetRoles] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles]([Id]),
    CONSTRAINT [FK_FolderShares_Locations] FOREIGN KEY ([LocationId]) REFERENCES [Locations]([Id]),
    CONSTRAINT [FK_FolderShares_User] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]),
    CONSTRAINT [FK_FolderShares_CreatedByUser] FOREIGN KEY ([CreatedBy]) REFERENCES [AspNetUsers]([Id])
);
CREATE INDEX [IX_FolderShares_FolderId] ON [dbo].[FolderShares] ([FolderId]);
CREATE INDEX [IX_FolderShares_AccessLevel] ON [dbo].[FolderShares] ([AccessLevel]);
CREATE INDEX [IX_FolderShares_RoleId] ON [dbo].[FolderShares] ([RoleId]);
CREATE INDEX [IX_FolderShares_LocationId] ON [dbo].[FolderShares] ([LocationId]);
CREATE INDEX [IX_FolderShares_UserId] ON [dbo].[FolderShares] ([UserId]);
GO

CREATE TABLE [dbo].[EmployeeRegistrationRequests] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [UserId] NVARCHAR(450) NOT NULL,
    [RequestedRoleId] NVARCHAR(450) NOT NULL,
    [RequestedLocationId] INT NOT NULL,
    [Status] TINYINT NOT NULL DEFAULT 0,
    [ReviewedByUserId] NVARCHAR(450) NULL,
    [ReviewedAtUtc] DATETIME2 NULL,
    [Notes] NVARCHAR(2000) NULL,
    [CreatedOn] DATETIME2 NOT NULL CONSTRAINT [DF_EmployeeRegistrationRequests_CreatedOn] DEFAULT SYSUTCDATETIME(),
    [CreatedBy] NVARCHAR(450) NULL,
    [LastModifiedOn] DATETIME2 NULL,
    [LastModifiedBy] NVARCHAR(450) NULL,
    [RecordStatusLIID] INT NOT NULL CONSTRAINT [DF_EmployeeRegistrationRequests_RecordStatusLIID] DEFAULT 1,
    CONSTRAINT [PK_EmployeeRegistrationRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Err_AspNetUsers] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Err_RequestedRole] FOREIGN KEY ([RequestedRoleId]) REFERENCES [AspNetRoles]([Id]),
    CONSTRAINT [FK_Err_RequestedLocation] FOREIGN KEY ([RequestedLocationId]) REFERENCES [Locations]([Id]),
    CONSTRAINT [FK_Err_Reviewer] FOREIGN KEY ([ReviewedByUserId]) REFERENCES [AspNetUsers]([Id])
);
CREATE INDEX [IX_Err_UserId] ON [dbo].[EmployeeRegistrationRequests] ([UserId]);
CREATE INDEX [IX_Err_Status] ON [dbo].[EmployeeRegistrationRequests] ([Status]);
GO

CREATE TABLE [dbo].[AuditLogs] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [UserId] NVARCHAR(450) NULL,
    [Action] NVARCHAR(100) NOT NULL,
    [EntityType] NVARCHAR(100) NULL,
    [EntityId] NVARCHAR(100) NULL,
    [Details] NVARCHAR(MAX) NULL,
    [CreatedOn] DATETIME2 NOT NULL CONSTRAINT [DF_AuditLogs_CreatedOn] DEFAULT SYSUTCDATETIME(),
    [CreatedBy] NVARCHAR(450) NULL,
    [LastModifiedOn] DATETIME2 NULL,
    [LastModifiedBy] NVARCHAR(450) NULL,
    [RecordStatusLIID] INT NOT NULL CONSTRAINT [DF_AuditLogs_RecordStatusLIID] DEFAULT 1,
    CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
);
CREATE INDEX [IX_AuditLogs_UserId] ON [dbo].[AuditLogs] ([UserId]);
GO
