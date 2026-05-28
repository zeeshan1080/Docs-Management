-- Seed roles and sample locations (run after 01-schema.sql)

SET NOCOUNT ON;

-- Roles (names must match app constants)
IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE NormalizedName = N'MANAGEMENT')
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES (NEWID(), N'Management', N'MANAGEMENT', NEWID());
IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE NormalizedName = N'STYLIST')
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES (NEWID(), N'Stylist', N'STYLIST', NEWID());
IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE NormalizedName = N'MEDSPA STAFF')
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES (NEWID(), N'Medspa Staff', N'MEDSPA STAFF', NEWID());
IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE NormalizedName = N'SPA STAFF')
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES (NEWID(), N'Spa Staff', N'SPA STAFF', NEWID());
IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE NormalizedName = N'OTHER')
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES (NEWID(), N'Other', N'OTHER', NEWID());

-- Sample locations
IF NOT EXISTS (SELECT 1 FROM Locations WHERE Name = N'Glen Mills')
    INSERT INTO Locations (Name, Code) VALUES (N'Glen Mills', N'');
IF NOT EXISTS (SELECT 1 FROM Locations WHERE Name = N'Headquarters')
    INSERT INTO Locations (Name, Code) VALUES (N'Headquarters', N'');
