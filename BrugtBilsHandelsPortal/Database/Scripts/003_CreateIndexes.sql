USE Brugtbilshandelsportal;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Users_UserGroupId' AND object_id = OBJECT_ID(N'dbo.Users'))
    CREATE INDEX IX_Users_UserGroupId ON dbo.Users(UserGroupId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Users_IsActive' AND object_id = OBJECT_ID(N'dbo.Users'))
    CREATE INDEX IX_Users_IsActive ON dbo.Users(IsActive);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Car_BrandId' AND object_id = OBJECT_ID(N'dbo.Car'))
    CREATE INDEX IX_Car_BrandId ON dbo.Car(BrandId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Car_Search' AND object_id = OBJECT_ID(N'dbo.Car'))
    CREATE INDEX IX_Car_Search ON dbo.Car(BrandId, ProductionYear, Mileage) INCLUDE (Model, Color);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Announcement_UserId' AND object_id = OBJECT_ID(N'dbo.Announcement'))
    CREATE INDEX IX_Announcement_UserId ON dbo.Announcement(UserId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Announcement_IsActive_CreatedAt' AND object_id = OBJECT_ID(N'dbo.Announcement'))
    CREATE INDEX IX_Announcement_IsActive_CreatedAt ON dbo.Announcement(IsActive, CreatedAt DESC) INCLUDE (UserId, CarId, Title, Price);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Announcement_IsActive_Price' AND object_id = OBJECT_ID(N'dbo.Announcement'))
    CREATE INDEX IX_Announcement_IsActive_Price ON dbo.Announcement(IsActive, Price) INCLUDE (CarId, Title, CreatedAt);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Images_AnnouncementId_SortOrder' AND object_id = OBJECT_ID(N'dbo.Images'))
    CREATE INDEX IX_Images_AnnouncementId_SortOrder ON dbo.Images(AnnouncementId, SortOrder, Id);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Images_OnePrimaryPerAnnouncement' AND object_id = OBJECT_ID(N'dbo.Images'))
    CREATE UNIQUE INDEX UX_Images_OnePrimaryPerAnnouncement ON dbo.Images(AnnouncementId) WHERE IsPrimary = 1;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Comments_Announcement_Parent_CreatedAt' AND object_id = OBJECT_ID(N'dbo.Comments'))
    CREATE INDEX IX_Comments_Announcement_Parent_CreatedAt ON dbo.Comments(AnnouncementId, ParentCommentId, CreatedAt) INCLUDE (UserId, IsDeleted);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Comments_UserId' AND object_id = OBJECT_ID(N'dbo.Comments'))
    CREATE INDEX IX_Comments_UserId ON dbo.Comments(UserId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Messages_Conversation' AND object_id = OBJECT_ID(N'dbo.Messages'))
    CREATE INDEX IX_Messages_Conversation ON dbo.Messages(AnnouncementId, SenderUserId, ReceiverUserId, CreatedAt);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Messages_Receiver_IsRead_CreatedAt' AND object_id = OBJECT_ID(N'dbo.Messages'))
    CREATE INDEX IX_Messages_Receiver_IsRead_CreatedAt ON dbo.Messages(ReceiverUserId, IsRead, CreatedAt DESC) INCLUDE (AnnouncementId, SenderUserId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Messages_Sender_CreatedAt' AND object_id = OBJECT_ID(N'dbo.Messages'))
    CREATE INDEX IX_Messages_Sender_CreatedAt ON dbo.Messages(SenderUserId, CreatedAt DESC) INCLUDE (AnnouncementId, ReceiverUserId, IsRead);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AuditLogs_CreatedAt' AND object_id = OBJECT_ID(N'dbo.AuditLogs'))
    CREATE INDEX IX_AuditLogs_CreatedAt ON dbo.AuditLogs(CreatedAt DESC);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AuditLogs_Entity' AND object_id = OBJECT_ID(N'dbo.AuditLogs'))
    CREATE INDEX IX_AuditLogs_Entity ON dbo.AuditLogs(EntityType, EntityId, CreatedAt DESC);
GO
