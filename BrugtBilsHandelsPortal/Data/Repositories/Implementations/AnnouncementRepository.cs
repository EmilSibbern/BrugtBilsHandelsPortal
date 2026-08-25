using System.Data.Common;
using BrugtBilsHandelsPortal.Data.Connections;
using BrugtBilsHandelsPortal.Data.Repositories.Interfaces;
using BrugtBilsHandelsPortal.DTOs.Announcements;
using BrugtBilsHandelsPortal.DTOs.Common;
using BrugtBilsHandelsPortal.Models.Entities;

namespace BrugtBilsHandelsPortal.Data.Repositories.Implementations;

/// <summary>Udfører parameteriserede SQL-operationer for biler og annoncer.</summary>
public class AnnouncementRepository : IAnnouncementRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public AnnouncementRepository(
        IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> CreateAsync(
        Car car,
        Announcement announcement)
    {
        const string createCarSql = """
                                    INSERT INTO dbo.Car
                                    (
                                        BrandId,
                                        Model,
                                        ProductionYear,
                                        Mileage,
                                        Color
                                    )
                                    OUTPUT INSERTED.CarId
                                    VALUES
                                    (
                                        @BrandId,
                                        @Model,
                                        @ProductionYear,
                                        @Mileage,
                                        @Color
                                    );
                                    """;

        const string createAnnouncementSql = """
                                             INSERT INTO dbo.Announcement
                                             (
                                                 UserId,
                                                 CarId,
                                                 Title,
                                                 Description,
                                                 Price
                                             )
                                             OUTPUT INSERTED.Id
                                             VALUES
                                             (
                                                 @UserId,
                                                 @CarId,
                                                 @Title,
                                                 @Description,
                                                 @Price
                                             );
                                             """;

        await using var connection =
            _connectionFactory.CreateConnection();

        await connection.OpenAsync();

        // Bil og annonce er én samlet oprettelse og må derfor lykkes eller rulles tilbage sammen.
        await using var transaction =
            await connection.BeginTransactionAsync();

        try
        {
            var carId = await InsertCarAsync(
                connection,
                transaction,
                createCarSql,
                car);

            announcement.CarId = carId;

            var announcementId =
                await InsertAnnouncementAsync(
                    connection,
                    transaction,
                    createAnnouncementSql,
                    announcement);

            await transaction.CommitAsync();

            return announcementId;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> UpdateAsync(
        Car car,
        Announcement announcement,
        int actorUserId,
        bool isAdministrator,
        DateTime? expectedUpdatedAt)
    {
        const string updateAnnouncementSql = """
                                             UPDATE a
                                             SET
                                                 Title = @Title,
                                                 Description = @Description,
                                                 Price = @Price,
                                                 UpdatedAt =
                                                     CASE
                                                         WHEN @UpdatedAt <=
                                                              COALESCE(a.UpdatedAt, a.CreatedAt)
                                                         THEN DATEADD(
                                                             SECOND,
                                                             1,
                                                             COALESCE(a.UpdatedAt, a.CreatedAt))
                                                         ELSE @UpdatedAt
                                                     END
                                             FROM dbo.Announcement AS a
                                             WHERE a.Id = @AnnouncementId
                                               AND
                                               (
                                                   a.UserId = @ActorUserId
                                                   OR @IsAdministrator = 1
                                               )
                                               AND
                                               (
                                                   (
                                                       a.UpdatedAt IS NULL
                                                       AND @ExpectedUpdatedAt IS NULL
                                                   )
                                                   OR a.UpdatedAt = @ExpectedUpdatedAt
                                               );
                                             """;

        const string updateCarSql = """
                                    UPDATE dbo.Car
                                    SET
                                        BrandId = @BrandId,
                                        Model = @Model,
                                        ProductionYear = @ProductionYear,
                                        Mileage = @Mileage,
                                        Color = @Color
                                    WHERE CarId = @CarId;
                                    """;

        await using var connection =
            _connectionFactory.CreateConnection();

        await connection.OpenAsync();

        await using var transaction =
            await connection.BeginTransactionAsync();

        try
        {
            await using var announcementCommand =
                connection.CreateCommand();

            announcementCommand.Transaction = transaction;
            announcementCommand.CommandText =
                updateAnnouncementSql;

            AddParameter(
                announcementCommand,
                "@AnnouncementId",
                announcement.Id);

            AddParameter(
                announcementCommand,
                "@ActorUserId",
                actorUserId);

            AddParameter(
                announcementCommand,
                "@IsAdministrator",
                isAdministrator);

            AddParameter(
                announcementCommand,
                "@ExpectedUpdatedAt",
                expectedUpdatedAt);

            AddParameter(
                announcementCommand,
                "@UpdatedAt",
                DateTime.UtcNow);

            AddParameter(
                announcementCommand,
                "@Title",
                announcement.Title);

            AddParameter(
                announcementCommand,
                "@Description",
                announcement.Description);

            AddParameter(
                announcementCommand,
                "@Price",
                announcement.Price);

            var updatedAnnouncements =
                await announcementCommand
                    .ExecuteNonQueryAsync();

            if (updatedAnnouncements != 1)
            {
                await transaction.RollbackAsync();
                return false;
            }

            await using var carCommand =
                connection.CreateCommand();

            carCommand.Transaction = transaction;
            carCommand.CommandText = updateCarSql;

            AddParameter(
                carCommand,
                "@CarId",
                car.CarId);

            AddParameter(
                carCommand,
                "@BrandId",
                car.BrandId);

            AddParameter(
                carCommand,
                "@Model",
                car.Model);

            AddParameter(
                carCommand,
                "@ProductionYear",
                car.ProductionYear);

            AddParameter(
                carCommand,
                "@Mileage",
                car.Mileage);

            AddParameter(
                carCommand,
                "@Color",
                car.Color);

            var updatedCars =
                await carCommand.ExecuteNonQueryAsync();

            if (updatedCars != 1)
            {
                throw new InvalidOperationException(
                    "Bilens oplysninger kunne ikke opdateres.");
            }

            await transaction.CommitAsync();

            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public Task<AnnouncementDetailsDto?> GetActiveByIdAsync(
        int announcementId)
    {
        return GetByIdInternalAsync(
            announcementId,
            onlyActive: true);
    }

    public Task<AnnouncementDetailsDto?> GetByIdAsync(
        int announcementId)
    {
        return GetByIdInternalAsync(
            announcementId,
            onlyActive: false);
    }

    // Den fælles forespørgsel sikrer, at aktive og privilegerede opslag mapper data ens.
    private async Task<AnnouncementDetailsDto?>
        GetByIdInternalAsync(
            int announcementId,
            bool onlyActive)
    {
        const string sql = """
                           SELECT TOP (1)
                               a.Id,
                               a.Title,
                               a.Description,
                               a.Price,
                               a.CreatedAt,
                               a.UpdatedAt,
                               a.IsActive,

                               a.UserId AS SellerUserId,
                               u.Username AS SellerUsername,

                               c.CarId,
                               c.BrandId,
                               cb.BrandName,
                               c.Model,
                               c.ProductionYear,
                               c.Mileage,
                               c.Color
                           FROM dbo.Announcement AS a
                           INNER JOIN dbo.Users AS u
                               ON u.Id = a.UserId
                           INNER JOIN dbo.Car AS c
                               ON c.CarId = a.CarId
                           INNER JOIN dbo.CarBrand AS cb
                               ON cb.BrandId = c.BrandId
                           WHERE a.Id = @AnnouncementId
                             AND
                             (
                                 @OnlyActive = 0
                                 OR a.IsActive = 1
                             )
                             AND u.IsActive = 1;
                           """;

        await using var connection =
            _connectionFactory.CreateConnection();

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        AddParameter(
            command,
            "@AnnouncementId",
            announcementId);

        AddParameter(
            command,
            "@OnlyActive",
            onlyActive);

        await using var reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        var colorOrdinal = reader.GetOrdinal("Color");
        var updatedAtOrdinal =
            reader.GetOrdinal("UpdatedAt");

        return new AnnouncementDetailsDto
        {
            Id = reader.GetInt32(
                reader.GetOrdinal("Id")),

            Title = reader.GetString(
                reader.GetOrdinal("Title")),

            Description = reader.GetString(
                reader.GetOrdinal("Description")),

            Price = reader.GetInt32(
                reader.GetOrdinal("Price")),

            CreatedAt = reader.GetDateTime(
                reader.GetOrdinal("CreatedAt")),
            UpdatedAt =
                reader.IsDBNull(updatedAtOrdinal)
                    ? null
                    : reader.GetDateTime(updatedAtOrdinal),

            IsActive = reader.GetBoolean(
                reader.GetOrdinal("IsActive")),

            SellerUserId = reader.GetInt32(
                reader.GetOrdinal("SellerUserId")),

            SellerUsername = reader.GetString(
                reader.GetOrdinal("SellerUsername")),

            CarId = reader.GetInt32(
                reader.GetOrdinal("CarId")),

            BrandId = reader.GetInt32(
                reader.GetOrdinal("BrandId")),

            BrandName = reader.GetString(
                reader.GetOrdinal("BrandName")),

            Model = reader.GetString(
                reader.GetOrdinal("Model")),

            ProductionYear = reader.GetInt16(
                reader.GetOrdinal("ProductionYear")),

            Mileage = reader.GetInt32(
                reader.GetOrdinal("Mileage")),

            Color = reader.IsDBNull(colorOrdinal)
                ? null
                : reader.GetString(colorOrdinal)
        };
    }

    public async Task<PagedResult<AnnouncementListItemDto>>
        SearchActiveAsync(
            AnnouncementSearchDto search)
    {
        const string filters = """
                               WHERE
                                   (
                                       @QueryPattern IS NULL
                                       OR Title LIKE @QueryPattern
                                       OR BrandName LIKE @QueryPattern
                                       OR Model LIKE @QueryPattern
                                   )
                                   AND
                                   (
                                       @BrandId IS NULL
                                       OR BrandId = @BrandId
                                   )
                                   AND
                                   (
                                       @MinPrice IS NULL
                                       OR Price >= @MinPrice
                                   )
                                   AND
                                   (
                                       @MaxPrice IS NULL
                                       OR Price <= @MaxPrice
                                   )
                                   AND
                                   (
                                       @MinProductionYear IS NULL
                                       OR ProductionYear >= @MinProductionYear
                                   )
                                   AND
                                   (
                                       @MaxMileage IS NULL
                                       OR Mileage <= @MaxMileage
                                   )
                               """;

        var orderBy = search.Sort switch
        {
            "price-asc" =>
                "Price ASC, AnnouncementId DESC",

            "price-desc" =>
                "Price DESC, AnnouncementId DESC",

            "mileage-asc" =>
                "Mileage ASC, AnnouncementId DESC",

            _ =>
                "CreatedAt DESC, AnnouncementId DESC"
        };

        var countSql = $"""
                        SELECT COUNT(*)
                        FROM dbo.vw_ActiveAnnouncements
                        {filters};
                        """;

        var resultSql = $"""
                         SELECT
                             AnnouncementId,
                             Title,
                             Price,
                             CreatedAt,
                             BrandName,
                             Model,
                             ProductionYear,
                             Mileage,
                             PrimaryImageFileName
                         FROM dbo.vw_ActiveAnnouncements
                         {filters}
                         ORDER BY {orderBy}
                         OFFSET @Offset ROWS
                         FETCH NEXT @PageSize ROWS ONLY;
                         """;

        await using var connection =
            _connectionFactory.CreateConnection();

        await connection.OpenAsync();

        var queryPattern =
            string.IsNullOrWhiteSpace(search.Query)
                ? null
                : $"%{search.Query.Trim()}%";

        int totalItems;

        await using (var countCommand =
                     connection.CreateCommand())
        {
            countCommand.CommandText = countSql;

            AddSearchParameters(
                countCommand,
                search,
                queryPattern);

            var countResult =
                await countCommand.ExecuteScalarAsync();

            totalItems = Convert.ToInt32(countResult);
        }

        var announcements =
            new List<AnnouncementListItemDto>();

        await using (var command =
                     connection.CreateCommand())
        {
            command.CommandText = resultSql;

            AddSearchParameters(
                command,
                search,
                queryPattern);

            AddParameter(
                command,
                "@Offset",
                (search.Page - 1) * search.PageSize);

            AddParameter(
                command,
                "@PageSize",
                search.PageSize);

            await using var reader =
                await command.ExecuteReaderAsync();

            var imageOrdinal =
                reader.GetOrdinal("PrimaryImageFileName");

            while (await reader.ReadAsync())
            {
                announcements.Add(
                    new AnnouncementListItemDto
                    {
                        Id = reader.GetInt32(
                            reader.GetOrdinal(
                                "AnnouncementId")),

                        Title = reader.GetString(
                            reader.GetOrdinal("Title")),

                        Price = reader.GetInt32(
                            reader.GetOrdinal("Price")),

                        CreatedAt = reader.GetDateTime(
                            reader.GetOrdinal("CreatedAt")),

                        BrandName = reader.GetString(
                            reader.GetOrdinal("BrandName")),

                        Model = reader.GetString(
                            reader.GetOrdinal("Model")),

                        ProductionYear = reader.GetInt16(
                            reader.GetOrdinal(
                                "ProductionYear")),

                        Mileage = reader.GetInt32(
                            reader.GetOrdinal("Mileage")),

                        PrimaryImageFileName =
                            reader.IsDBNull(imageOrdinal)
                                ? null
                                : reader.GetString(
                                    imageOrdinal)
                    });
            }
        }

        return new PagedResult<AnnouncementListItemDto>
        {
            Items = announcements,
            Page = search.Page,
            PageSize = search.PageSize,
            TotalItems = totalItems
        };
    }

    private static async Task<int> InsertCarAsync(
        DbConnection connection,
        DbTransaction transaction,
        string sql,
        Car car)
    {
        await using var command = connection.CreateCommand();

        command.Transaction = transaction;
        command.CommandText = sql;

        AddParameter(command, "@BrandId", car.BrandId);
        AddParameter(command, "@Model", car.Model);

        AddParameter(
            command,
            "@ProductionYear",
            car.ProductionYear);

        AddParameter(command, "@Mileage", car.Mileage);
        AddParameter(command, "@Color", car.Color);

        var result = await command.ExecuteScalarAsync();

        if (result is null || result is DBNull)
        {
            throw new InvalidOperationException(
                "Bilen kunne ikke oprettes.");
        }

        return Convert.ToInt32(result);
    }

    private static async Task<int> InsertAnnouncementAsync(
        DbConnection connection,
        DbTransaction transaction,
        string sql,
        Announcement announcement)
    {
        await using var command = connection.CreateCommand();

        command.Transaction = transaction;
        command.CommandText = sql;

        AddParameter(
            command,
            "@UserId",
            announcement.UserId);

        AddParameter(
            command,
            "@CarId",
            announcement.CarId);

        AddParameter(
            command,
            "@Title",
            announcement.Title);

        AddParameter(
            command,
            "@Description",
            announcement.Description);

        AddParameter(
            command,
            "@Price",
            announcement.Price);

        var result = await command.ExecuteScalarAsync();

        if (result is null || result is DBNull)
        {
            throw new InvalidOperationException(
                "Annoncen kunne ikke oprettes.");
        }

        return Convert.ToInt32(result);
    }

    private static void AddParameter(
        DbCommand command,
        string name,
        object? value)
    {
        var parameter = command.CreateParameter();

        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;

        command.Parameters.Add(parameter);
    }

    private static void AddSearchParameters(
        DbCommand command,
        AnnouncementSearchDto search,
        string? queryPattern)
    {
        AddParameter(
            command,
            "@QueryPattern",
            queryPattern);

        AddParameter(
            command,
            "@BrandId",
            search.BrandId);

        AddParameter(
            command,
            "@MinPrice",
            search.MinPrice);

        AddParameter(
            command,
            "@MaxPrice",
            search.MaxPrice);

        AddParameter(
            command,
            "@MinProductionYear",
            search.MinProductionYear);

        AddParameter(
            command,
            "@MaxMileage",
            search.MaxMileage);
    }
}
