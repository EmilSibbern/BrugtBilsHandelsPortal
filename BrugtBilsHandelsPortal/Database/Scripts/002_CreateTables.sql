USE Brugtbilshandelsportal;
GO

IF OBJECT_ID(N'dbo.UserGroups', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserGroups
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_UserGroups PRIMARY KEY,
        Name nvarchar(50) NOT NULL,
        CONSTRAINT UQ_UserGroups_Name UNIQUE (Name),
        CONSTRAINT CK_UserGroups_Name_NotBlank CHECK (LEN(LTRIM(RTRIM(Name))) > 0)
    );
END;
GO
IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
        Username nvarchar(50) NOT NULL,
        PasswordHash nvarchar(500) NOT NULL,
        Email nvarchar(254) NOT NULL,
        UserGroupId int NOT NULL,
        Phone nvarchar(30) NULL,
        Address nvarchar(250) NULL,
        IsActive bit NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
        CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt datetime2(0) NULL,
        CONSTRAINT UQ_Users_Username UNIQUE (Username),
        CONSTRAINT UQ_Users_Email UNIQUE (Email),
        CONSTRAINT FK_Users_UserGroups FOREIGN KEY (UserGroupId) REFERENCES dbo.UserGroups(Id),
        CONSTRAINT CK_Users_Username_NotBlank CHECK (LEN(LTRIM(RTRIM(Username))) BETWEEN 3 AND 50),
        CONSTRAINT CK_Users_Email_NotBlank CHECK (LEN(LTRIM(RTRIM(Email))) >= 5),
        CONSTRAINT CK_Users_PasswordHash_NotBlank CHECK (LEN(LTRIM(RTRIM(PasswordHash))) > 0)
    );
END;
GO

IF COL_LENGTH(N'dbo.Users', N'UpdatedAt') IS NULL
    ALTER TABLE dbo.Users ADD UpdatedAt datetime2(0) NULL;
GO

IF OBJECT_ID(N'dbo.CarBrand', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CarBrand
    (
        BrandId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_CarBrand PRIMARY KEY,
        BrandName nvarchar(100) NOT NULL,
        CONSTRAINT UQ_CarBrand_BrandName UNIQUE (BrandName),
        CONSTRAINT CK_CarBrand_BrandName_NotBlank CHECK (LEN(LTRIM(RTRIM(BrandName))) > 0)
    );
END;
GO

IF OBJECT_ID(N'dbo.Car', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Car
    (
        CarId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Car PRIMARY KEY,
        BrandId int NOT NULL,
        Model nvarchar(100) NOT NULL,
        ProductionYear smallint NOT NULL,
        Mileage int NOT NULL,
        Color nvarchar(50) NULL,
        CONSTRAINT FK_Car_CarBrand FOREIGN KEY (BrandId) REFERENCES dbo.CarBrand(BrandId),
        CONSTRAINT CK_Car_Model_NotBlank CHECK (LEN(LTRIM(RTRIM(Model))) > 0),
        CONSTRAINT CK_Car_ProductionYear CHECK (ProductionYear BETWEEN 1886 AND 2100),
        CONSTRAINT CK_Car_Mileage CHECK (Mileage >= 0)
    );
END;
GO

IF OBJECT_ID(N'dbo.Announcement', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Announcement
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Announcement PRIMARY KEY,
        UserId int NOT NULL,
        CarId int NOT NULL,
        Title nvarchar(150) NOT NULL,
        Description nvarchar(max) NOT NULL,
        Price int NOT NULL,
        CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_Announcement_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt datetime2(0) NULL,
        IsActive bit NOT NULL CONSTRAINT DF_Announcement_IsActive DEFAULT (1),
        CONSTRAINT UQ_Announcement_CarId UNIQUE (CarId),
        CONSTRAINT FK_Announcement_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id),
        CONSTRAINT FK_Announcement_Car FOREIGN KEY (CarId) REFERENCES dbo.Car(CarId),
        CONSTRAINT CK_Announcement_Title_NotBlank CHECK (LEN(LTRIM(RTRIM(Title))) BETWEEN 3 AND 150),
        CONSTRAINT CK_Announcement_Description_NotBlank CHECK (LEN(LTRIM(RTRIM(Description))) > 0),
        CONSTRAINT CK_Announcement_Price CHECK (Price > 0)
    );
END;
GO

IF OBJECT_ID(N'dbo.Images', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Images
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Images PRIMARY KEY,
        AnnouncementId int NOT NULL,
        FileName nvarchar(255) NOT NULL,
        IsPrimary bit NOT NULL CONSTRAINT DF_Images_IsPrimary DEFAULT (0),
        SortOrder int NOT NULL CONSTRAINT DF_Images_SortOrder DEFAULT (0),
        CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_Images_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_Images_Announcement_FileName UNIQUE (AnnouncementId, FileName),
        CONSTRAINT FK_Images_Announcement FOREIGN KEY (AnnouncementId) REFERENCES dbo.Announcement(Id),
        CONSTRAINT CK_Images_FileName_NotBlank CHECK (LEN(LTRIM(RTRIM(FileName))) > 0),
        CONSTRAINT CK_Images_SortOrder CHECK (SortOrder >= 0)
    );
END;
GO

IF OBJECT_ID(N'dbo.Comments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Comments
    (
        Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Comments PRIMARY KEY,
        AnnouncementId int NOT NULL,
        UserId int NOT NULL,
        Comment nvarchar(1500) NOT NULL,
        CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_Comments_CreatedAt DEFAULT (SYSUTCDATETIME()),
        ParentCommentId bigint NULL,
        IsDeleted bit NOT NULL CONSTRAINT DF_Comments_IsDeleted DEFAULT (0),
        DeletedAt datetime2(0) NULL,
        DeletedByUserId int NULL,
        CONSTRAINT FK_Comments_Announcement FOREIGN KEY (AnnouncementId) REFERENCES dbo.Announcement(Id),
        CONSTRAINT FK_Comments_User FOREIGN KEY (UserId) REFERENCES dbo.Users(Id),
        CONSTRAINT FK_Comments_ParentComment FOREIGN KEY (ParentCommentId) REFERENCES dbo.Comments(Id),
        CONSTRAINT FK_Comments_DeletedByUser FOREIGN KEY (DeletedByUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_Comments_Comment_NotBlank CHECK (LEN(LTRIM(RTRIM(Comment))) BETWEEN 1 AND 1500),
        CONSTRAINT CK_Comments_NotOwnParent CHECK (ParentCommentId IS NULL OR ParentCommentId <> Id),
        CONSTRAINT CK_Comments_DeleteMetadata CHECK
        (
            (IsDeleted = 0 AND DeletedAt IS NULL AND DeletedByUserId IS NULL)
            OR (IsDeleted = 1 AND DeletedAt IS NOT NULL AND DeletedByUserId IS NOT NULL)
        )
    );
END;
GO

IF OBJECT_ID(N'dbo.Messages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Messages
    (
        Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Messages PRIMARY KEY,
        AnnouncementId int NOT NULL,
        SenderUserId int NOT NULL,
        ReceiverUserId int NOT NULL,
        Message nvarchar(2000) NOT NULL,
        CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_Messages_CreatedAt DEFAULT (SYSUTCDATETIME()),
        IsRead bit NOT NULL CONSTRAINT DF_Messages_IsRead DEFAULT (0),
        ReadAt datetime2(0) NULL,
        CONSTRAINT FK_Messages_Announcement FOREIGN KEY (AnnouncementId) REFERENCES dbo.Announcement(Id),
        CONSTRAINT FK_Messages_SenderUser FOREIGN KEY (SenderUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT FK_Messages_ReceiverUser FOREIGN KEY (ReceiverUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_Messages_Message_NotBlank CHECK (LEN(LTRIM(RTRIM(Message))) BETWEEN 1 AND 2000),
        CONSTRAINT CK_Messages_ReadAt CHECK
        (
            (IsRead = 0 AND ReadAt IS NULL)
            OR (IsRead = 1 AND ReadAt IS NOT NULL)
        )
    );
END;
GO

IF OBJECT_ID(N'dbo.AuditLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditLogs
    (
        Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_AuditLogs PRIMARY KEY,
        UserId int NULL,
        Action nvarchar(100) NOT NULL,
        EntityType nvarchar(100) NOT NULL,
        EntityId nvarchar(100) NULL,
        Details nvarchar(2000) NULL,
        CorrelationId uniqueidentifier NULL,
        CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_AuditLogs_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_AuditLogs_User FOREIGN KEY (UserId) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_AuditLogs_Action_NotBlank CHECK (LEN(LTRIM(RTRIM(Action))) > 0),
        CONSTRAINT CK_AuditLogs_EntityType_NotBlank CHECK (LEN(LTRIM(RTRIM(EntityType))) > 0)
    );
END;
GO
