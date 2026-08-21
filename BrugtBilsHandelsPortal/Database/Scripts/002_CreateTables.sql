USE Brugtbilshandelsportal;
GO

IF OBJECT_ID(N'dbo.UserGroups', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.UserGroups
        (
            Id   int IDENTITY(1,1) NOT NULL,
            Name nvarchar(50) NOT NULL,

            CONSTRAINT PK_UserGroups
                PRIMARY KEY (Id),

            CONSTRAINT UQ_UserGroups_Name
                UNIQUE (Name)
        );
    END;
GO

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.Users
        (
            Id           int IDENTITY(1,1) NOT NULL,
            Username     nvarchar(50) NOT NULL,
            PasswordHash nvarchar(500) NOT NULL,
            Email        nvarchar(254) NOT NULL,
            UserGroupId  int NOT NULL,
            Phone        nvarchar(30) NULL,
            Address      nvarchar(250) NULL,
            IsActive     bit NOT NULL
                CONSTRAINT DF_Users_IsActive DEFAULT (1),
            CreatedAt    datetime2 NOT NULL
                CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSUTCDATETIME()),

            CONSTRAINT PK_Users
                PRIMARY KEY (Id),

            CONSTRAINT UQ_Users_Username
                UNIQUE (Username),

            CONSTRAINT UQ_Users_Email
                UNIQUE (Email),

            CONSTRAINT FK_Users_UserGroups
                FOREIGN KEY (UserGroupId)
                    REFERENCES dbo.UserGroups(Id)
        );
    END;
GO