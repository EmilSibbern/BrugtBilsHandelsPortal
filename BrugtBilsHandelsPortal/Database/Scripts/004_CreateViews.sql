USE Brugtbilshandelsportal;
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
