USE master;
GO

IF DB_ID(N'Brugtbilshandelsportal') IS NULL
BEGIN
    CREATE DATABASE Brugtbilshandelsportal;
END;
GO

ALTER DATABASE Brugtbilshandelsportal SET RECOVERY SIMPLE;
GO
