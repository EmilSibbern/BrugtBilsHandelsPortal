USE Brugtbilshandelsportal;
GO

IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'IX_Users_UserGroupId'
          AND object_id = OBJECT_ID(N'dbo.Users')
    )
    BEGIN
        CREATE INDEX IX_Users_UserGroupId
            ON dbo.Users(UserGroupId);
    END;
GO

IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'IX_Users_IsActive'
          AND object_id = OBJECT_ID(N'dbo.Users')
    )
    BEGIN
        CREATE INDEX IX_Users_IsActive
            ON dbo.Users(IsActive);
    END;
GO