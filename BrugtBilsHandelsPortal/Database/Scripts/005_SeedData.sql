USE Brugtbilshandelsportal;
GO

INSERT INTO dbo.UserGroups (Name)
SELECT seed.Name
FROM (VALUES (N'User'), (N'Administrator')) AS seed(Name)
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.UserGroups AS existing WHERE existing.Name = seed.Name
);
GO

INSERT INTO dbo.CarBrand (BrandName)
SELECT seed.BrandName
FROM
(
    VALUES
        (N'Alfa Romeo'), (N'Audi'), (N'BMW'), (N'Chevrolet'),
        (N'Citroën'), (N'Dacia'), (N'Fiat'), (N'Ford'),
        (N'Honda'), (N'Hyundai'), (N'Kia'), (N'Mazda'),
        (N'Mercedes-Benz'), (N'Mitsubishi'), (N'Nissan'), (N'Opel'),
        (N'Peugeot'), (N'Polestar'), (N'Renault'), (N'Seat'),
        (N'Skoda'), (N'Suzuki'), (N'Tesla'), (N'Toyota'),
        (N'Volkswagen'), (N'Volvo')
) AS seed(BrandName)
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.CarBrand AS existing
    WHERE existing.BrandName = seed.BrandName
);
GO
