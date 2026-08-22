USE Brugtbilshandelsportal;
GO

DECLARE @TableName sysname =
    CASE
        WHEN OBJECT_ID(N'dbo.Announcement', N'U') IS NOT NULL THEN N'Announcement'
        WHEN OBJECT_ID(N'dbo.Announce', N'U') IS NOT NULL THEN N'Announce'
    END;

IF @TableName IS NOT NULL
BEGIN
    DECLARE @ObjectId int = OBJECT_ID(N'dbo.' + @TableName);
    DECLARE @PriceIsInt bit = 0;

    SELECT @PriceIsInt = CASE WHEN type_definition.name = N'int' THEN 1 ELSE 0 END
    FROM sys.columns AS column_definition
    INNER JOIN sys.types AS type_definition
        ON type_definition.user_type_id = column_definition.user_type_id
    WHERE column_definition.object_id = @ObjectId
      AND column_definition.name = N'Price';

    IF @PriceIsInt = 0
    BEGIN
        DECLARE @HasUnsafePrices bit = 0;
        DECLARE @CheckSql nvarchar(max) =
            N'SELECT @Result = CASE WHEN EXISTS
              (SELECT 1 FROM dbo.' + QUOTENAME(@TableName) + N'
               WHERE Price <> FLOOR(Price) OR Price < 1 OR Price > 2147483647)
              THEN 1 ELSE 0 END;';

        EXEC sys.sp_executesql
            @CheckSql,
            N'@Result bit OUTPUT',
            @Result = @HasUnsafePrices OUTPUT;

        IF @HasUnsafePrices = 1
        BEGIN
            THROW 50001,
                'Price contains values that cannot be converted safely to whole kroner.',
                1;
        END;

        DECLARE @LegacyPrefix sysname =
            CASE WHEN @TableName = N'Announce' THEN N'Announce' ELSE N'Announcement' END;

        DECLARE @MigrationSql nvarchar(max) =
            N'DROP INDEX IF EXISTS IX_' + @LegacyPrefix + N'_IsActive_CreatedAt ON dbo.' + QUOTENAME(@TableName) + N';
              DROP INDEX IF EXISTS IX_' + @LegacyPrefix + N'_IsActive_Price ON dbo.' + QUOTENAME(@TableName) + N';
              IF OBJECT_ID(N''dbo.CK_' + @LegacyPrefix + N'_Price'', N''C'') IS NOT NULL
                  ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' DROP CONSTRAINT CK_' + @LegacyPrefix + N'_Price;
              ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN Price int NOT NULL;
              ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' WITH CHECK
                  ADD CONSTRAINT CK_' + @LegacyPrefix + N'_Price CHECK (Price > 0);';

        EXEC sys.sp_executesql @MigrationSql;
    END;
END;
GO

