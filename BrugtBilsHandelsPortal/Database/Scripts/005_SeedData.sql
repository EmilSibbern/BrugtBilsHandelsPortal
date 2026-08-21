USE Brugtbilshandelsportal;
GO

IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.UserGroups
        WHERE Name = N'User'
    )
    BEGIN
        INSERT INTO dbo.UserGroups (Name)
        VALUES (N'User');
    END;
GO

IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.UserGroups
        WHERE Name = N'Administrator'
    )
    BEGIN
        INSERT INTO dbo.UserGroups (Name)
        VALUES (N'Administrator');
    END;
GO