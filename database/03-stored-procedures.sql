-- Permission resolution and audit logging are implemented in the application (EF Core).
-- Run this script once on databases that were created with older versions that installed
-- TVFs and stored procedures for Dapper-based access control.

IF OBJECT_ID(N'dbo.usp_GetDocumentShareIdsInFolderForUser', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_GetDocumentShareIdsInFolderForUser;
IF OBJECT_ID(N'dbo.usp_GetViewOnlyFolderIdsForUser', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_GetViewOnlyFolderIdsForUser;
IF OBJECT_ID(N'dbo.usp_GetFullAccessFolderIdsForUser', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_GetFullAccessFolderIdsForUser;
IF OBJECT_ID(N'dbo.usp_GetAccessibleDocumentIdsForUser', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_GetAccessibleDocumentIdsForUser;
IF OBJECT_ID(N'dbo.usp_GetAccessibleFolderIdsForUser', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_GetAccessibleFolderIdsForUser;
IF OBJECT_ID(N'dbo.usp_InsertAuditLog', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_InsertAuditLog;
IF OBJECT_ID(N'dbo.ufn_NavigableFolderIdsForUser', N'TF') IS NOT NULL
    DROP FUNCTION dbo.ufn_NavigableFolderIdsForUser;
IF OBJECT_ID(N'dbo.ufn_ViewOnlyFolderIdsForUser', N'TF') IS NOT NULL
    DROP FUNCTION dbo.ufn_ViewOnlyFolderIdsForUser;
IF OBJECT_ID(N'dbo.ufn_FullAccessFolderIdsForUser', N'TF') IS NOT NULL
    DROP FUNCTION dbo.ufn_FullAccessFolderIdsForUser;
IF OBJECT_ID(N'dbo.ufn_AccessibleFolderIdsForUser', N'TF') IS NOT NULL
    DROP FUNCTION dbo.ufn_AccessibleFolderIdsForUser;
GO
