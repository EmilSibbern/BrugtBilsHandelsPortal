USE Brugtbilshandelsportal;
GO

IF OBJECT_ID(N'dbo.Announce', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.Announcement', N'U') IS NOT NULL
BEGIN
    THROW 50002,
        'Both dbo.Announce and dbo.Announcement exist. The rename cannot continue safely.',
        1;
END;
GO

IF OBJECT_ID(N'dbo.Announce', N'U') IS NOT NULL
BEGIN
    EXEC sys.sp_rename N'dbo.Announce', N'Announcement';
END;
GO

IF COL_LENGTH(N'dbo.Images', N'AnnounceId') IS NOT NULL
   AND COL_LENGTH(N'dbo.Images', N'AnnouncementId') IS NULL
    EXEC sys.sp_rename N'dbo.Images.AnnounceId', N'AnnouncementId', N'COLUMN';
GO

IF COL_LENGTH(N'dbo.Comments', N'AnnounceId') IS NOT NULL
   AND COL_LENGTH(N'dbo.Comments', N'AnnouncementId') IS NULL
    EXEC sys.sp_rename N'dbo.Comments.AnnounceId', N'AnnouncementId', N'COLUMN';
GO

IF COL_LENGTH(N'dbo.Messages', N'AnnounceId') IS NOT NULL
   AND COL_LENGTH(N'dbo.Messages', N'AnnouncementId') IS NULL
    EXEC sys.sp_rename N'dbo.Messages.AnnounceId', N'AnnouncementId', N'COLUMN';
GO

IF OBJECT_ID(N'dbo.PK_Announce', N'PK') IS NOT NULL
    EXEC sys.sp_rename N'dbo.PK_Announce', N'PK_Announcement', N'OBJECT';
IF OBJECT_ID(N'dbo.DF_Announce_CreatedAt', N'D') IS NOT NULL
    EXEC sys.sp_rename N'dbo.DF_Announce_CreatedAt', N'DF_Announcement_CreatedAt', N'OBJECT';
IF OBJECT_ID(N'dbo.DF_Announce_IsActive', N'D') IS NOT NULL
    EXEC sys.sp_rename N'dbo.DF_Announce_IsActive', N'DF_Announcement_IsActive', N'OBJECT';
IF OBJECT_ID(N'dbo.UQ_Announce_CarId', N'UQ') IS NOT NULL
    EXEC sys.sp_rename N'dbo.UQ_Announce_CarId', N'UQ_Announcement_CarId', N'OBJECT';
IF OBJECT_ID(N'dbo.FK_Announce_Users', N'F') IS NOT NULL
    EXEC sys.sp_rename N'dbo.FK_Announce_Users', N'FK_Announcement_Users', N'OBJECT';
IF OBJECT_ID(N'dbo.FK_Announce_Car', N'F') IS NOT NULL
    EXEC sys.sp_rename N'dbo.FK_Announce_Car', N'FK_Announcement_Car', N'OBJECT';
IF OBJECT_ID(N'dbo.CK_Announce_Title_NotBlank', N'C') IS NOT NULL
    EXEC sys.sp_rename N'dbo.CK_Announce_Title_NotBlank', N'CK_Announcement_Title_NotBlank', N'OBJECT';
IF OBJECT_ID(N'dbo.CK_Announce_Description_NotBlank', N'C') IS NOT NULL
    EXEC sys.sp_rename N'dbo.CK_Announce_Description_NotBlank', N'CK_Announcement_Description_NotBlank', N'OBJECT';
IF OBJECT_ID(N'dbo.CK_Announce_Price', N'C') IS NOT NULL
    EXEC sys.sp_rename N'dbo.CK_Announce_Price', N'CK_Announcement_Price', N'OBJECT';
IF OBJECT_ID(N'dbo.UQ_Images_Announce_FileName', N'UQ') IS NOT NULL
    EXEC sys.sp_rename N'dbo.UQ_Images_Announce_FileName', N'UQ_Images_Announcement_FileName', N'OBJECT';
IF OBJECT_ID(N'dbo.FK_Images_Announce', N'F') IS NOT NULL
    EXEC sys.sp_rename N'dbo.FK_Images_Announce', N'FK_Images_Announcement', N'OBJECT';
IF OBJECT_ID(N'dbo.FK_Comments_Announce', N'F') IS NOT NULL
    EXEC sys.sp_rename N'dbo.FK_Comments_Announce', N'FK_Comments_Announcement', N'OBJECT';
IF OBJECT_ID(N'dbo.FK_Messages_Announce', N'F') IS NOT NULL
    EXEC sys.sp_rename N'dbo.FK_Messages_Announce', N'FK_Messages_Announcement', N'OBJECT';
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Announcement') AND name = N'IX_Announce_UserId')
    EXEC sys.sp_rename N'dbo.Announcement.IX_Announce_UserId', N'IX_Announcement_UserId', N'INDEX';
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Announcement') AND name = N'IX_Announce_IsActive_CreatedAt')
    EXEC sys.sp_rename N'dbo.Announcement.IX_Announce_IsActive_CreatedAt', N'IX_Announcement_IsActive_CreatedAt', N'INDEX';
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Announcement') AND name = N'IX_Announce_IsActive_Price')
    EXEC sys.sp_rename N'dbo.Announcement.IX_Announce_IsActive_Price', N'IX_Announcement_IsActive_Price', N'INDEX';
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Images') AND name = N'IX_Images_AnnounceId_SortOrder')
    EXEC sys.sp_rename N'dbo.Images.IX_Images_AnnounceId_SortOrder', N'IX_Images_AnnouncementId_SortOrder', N'INDEX';
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Images') AND name = N'UX_Images_OnePrimaryPerAnnounce')
    EXEC sys.sp_rename N'dbo.Images.UX_Images_OnePrimaryPerAnnounce', N'UX_Images_OnePrimaryPerAnnouncement', N'INDEX';
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Comments') AND name = N'IX_Comments_Announce_Parent_CreatedAt')
    EXEC sys.sp_rename N'dbo.Comments.IX_Comments_Announce_Parent_CreatedAt', N'IX_Comments_Announcement_Parent_CreatedAt', N'INDEX';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Announcement') AND name = N'IX_Announcement_UserId')
    CREATE INDEX IX_Announcement_UserId ON dbo.Announcement(UserId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Announcement') AND name = N'IX_Announcement_IsActive_CreatedAt')
    CREATE INDEX IX_Announcement_IsActive_CreatedAt ON dbo.Announcement(IsActive, CreatedAt DESC) INCLUDE (UserId, CarId, Title, Price);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Announcement') AND name = N'IX_Announcement_IsActive_Price')
    CREATE INDEX IX_Announcement_IsActive_Price ON dbo.Announcement(IsActive, Price) INCLUDE (CarId, Title, CreatedAt);
GO

CREATE OR ALTER VIEW dbo.vw_ActiveAnnouncements
AS
    SELECT
        a.Id AS AnnouncementId,
        a.Title,
        a.Description,
        a.Price,
        a.CreatedAt,
        a.UserId AS SellerUserId,
        u.Username AS SellerUsername,
        c.CarId,
        cb.BrandId,
        cb.BrandName,
        c.Model,
        c.ProductionYear,
        c.Mileage,
        c.Color,
        pi.Id AS PrimaryImageId,
        pi.FileName AS PrimaryImageFileName
    FROM dbo.Announcement AS a
    INNER JOIN dbo.Users AS u ON u.Id = a.UserId AND u.IsActive = 1
    INNER JOIN dbo.Car AS c ON c.CarId = a.CarId
    INNER JOIN dbo.CarBrand AS cb ON cb.BrandId = c.BrandId
    OUTER APPLY
    (
        SELECT TOP (1) i.Id, i.FileName
        FROM dbo.Images AS i
        WHERE i.AnnouncementId = a.Id
        ORDER BY i.IsPrimary DESC, i.SortOrder, i.Id
    ) AS pi
    WHERE a.IsActive = 1;
GO
