using System.Data;
using System.Data.Common;
using BrugtBilsHandelsPortal.Data.Connections;
using BrugtBilsHandelsPortal.Data.Repositories.Interfaces;
using BrugtBilsHandelsPortal.DTOs.Images;
using BrugtBilsHandelsPortal.Models.Entities;

namespace BrugtBilsHandelsPortal.Data.Repositories.Implementations;

/// <summary>Vedligeholder billedmetadata og ejerbegrænsede billedændringer atomisk.</summary>
public class ImageRepository : IImageRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ImageRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<ImageMetadataCreateResult> AddForOwnerAsync(int announcementId, int userId, string fileName, int maxImages)
    {
        const string lockAnnouncementSql = """
                                           SELECT Id
                                           FROM dbo.Announcement
                                                WITH (UPDLOCK, HOLDLOCK)
                                           WHERE Id = @AnnouncementId
                                             AND UserId = @UserId;
                                           """;

        const string countImagesSql = """
                                      SELECT COUNT(*)
                                      FROM dbo.Images
                                      WHERE AnnouncementId =
                                            @AnnouncementId;
                                      """;

        const string insertSql = """
                                 INSERT INTO dbo.Images
                                 (
                                     AnnouncementId,
                                     FileName,
                                     IsPrimary,
                                     SortOrder
                                 )
                                 OUTPUT
                                     INSERTED.Id,
                                     INSERTED.AnnouncementId,
                                     INSERTED.FileName,
                                     INSERTED.IsPrimary,
                                     INSERTED.SortOrder,
                                     INSERTED.CreatedAt
                                 VALUES
                                 (
                                     @AnnouncementId,
                                     @FileName,
                                     @IsPrimary,
                                     @SortOrder
                                 );
                                 """;

        await using var connection = _connectionFactory.CreateConnection();

        await connection.OpenAsync();

        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            var ownsAnnouncement = await OwnsAnnouncementAsync(connection, transaction, lockAnnouncementSql, announcementId, userId);

            if (!ownsAnnouncement)
            {
                await transaction.CommitAsync();

                return ImageMetadataCreateResult.Failed(ImageMetadataCreateStatus.AnnouncementNotFoundOrNotOwned);
            }

            var imageCount = await GetImageCountAsync(connection, transaction, countImagesSql, announcementId);

            if (imageCount >= maxImages)
            {
                await transaction.CommitAsync();

                return ImageMetadataCreateResult.Failed(ImageMetadataCreateStatus.ImageLimitReached);
            }

            var image = await InsertImageAsync(connection, transaction, insertSql, announcementId, fileName, isPrimary: imageCount == 0, sortOrder: imageCount);

            await transaction.CommitAsync();

            return ImageMetadataCreateResult.Successful(image);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<IReadOnlyList<AnnouncementImage>> GetForActiveAnnouncementAsync(int announcementId)
    {
        const string sql = """
                           SELECT
                               i.Id,
                               i.AnnouncementId,
                               i.FileName,
                               i.IsPrimary,
                               i.SortOrder,
                               i.CreatedAt
                           FROM dbo.Images AS i
                           INNER JOIN dbo.Announcement AS a
                               ON a.Id = i.AnnouncementId
                           INNER JOIN dbo.Users AS u
                               ON u.Id = a.UserId
                           WHERE i.AnnouncementId =
                                 @AnnouncementId
                             AND a.IsActive = 1
                             AND u.IsActive = 1
                           ORDER BY
                               i.IsPrimary DESC,
                               i.SortOrder,
                               i.Id;
                           """;

        var images = new List<AnnouncementImage>();

        await using var connection = _connectionFactory.CreateConnection();

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        AddParameter(command, "@AnnouncementId", announcementId);

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            images.Add(ReadImage(reader));
        }

        return images;
    }

    public async Task<bool> SetPrimaryForOwnerAsync(int announcementId, int imageId, int userId)
    {
        const string validateSql = """
                                   SELECT i.Id
                                   FROM dbo.Images AS i
                                        WITH (UPDLOCK, HOLDLOCK)
                                   INNER JOIN dbo.Announcement AS a
                                       ON a.Id = i.AnnouncementId
                                   WHERE i.Id = @ImageId
                                     AND i.AnnouncementId = @AnnouncementId
                                     AND a.UserId = @UserId;
                                   """;

        const string clearPrimarySql = """
                                       UPDATE dbo.Images
                                       SET IsPrimary = 0
                                       WHERE AnnouncementId = @AnnouncementId
                                         AND IsPrimary = 1;
                                       """;

        const string setPrimarySql = """
                                     UPDATE dbo.Images
                                     SET IsPrimary = 1
                                     WHERE Id = @ImageId
                                       AND AnnouncementId = @AnnouncementId;
                                     """;

        await using var connection = _connectionFactory.CreateConnection();

        await connection.OpenAsync();

        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            await using var validateCommand = connection.CreateCommand();

            validateCommand.Transaction = transaction;
            validateCommand.CommandText = validateSql;

            AddParameter(validateCommand, "@ImageId", imageId);
            AddParameter(validateCommand, "@AnnouncementId", announcementId);
            AddParameter(validateCommand, "@UserId", userId);

            var matchingImage = await validateCommand.ExecuteScalarAsync();

            if (matchingImage is null || matchingImage is DBNull)
            {
                await transaction.CommitAsync();
                return false;
            }

            await using var clearCommand = connection.CreateCommand();

            clearCommand.Transaction = transaction;
            clearCommand.CommandText = clearPrimarySql;

            AddParameter(clearCommand, "@AnnouncementId", announcementId);

            await clearCommand.ExecuteNonQueryAsync();

            await using var setCommand = connection.CreateCommand();

            setCommand.Transaction = transaction;
            setCommand.CommandText = setPrimarySql;

            AddParameter(setCommand, "@ImageId", imageId);
            AddParameter(setCommand, "@AnnouncementId", announcementId);

            var affectedRows = await setCommand.ExecuteNonQueryAsync();

            await transaction.CommitAsync();

            return affectedRows == 1;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<string?> DeleteForOwnerAsync(int announcementId, int imageId, int userId)
    {
        const string selectImageSql = """
                                      SELECT TOP (1)
                                          i.FileName,
                                          i.IsPrimary
                                      FROM dbo.Images AS i
                                          WITH (UPDLOCK, HOLDLOCK)
                                      INNER JOIN dbo.Announcement AS a
                                          ON a.Id = i.AnnouncementId
                                      WHERE i.Id = @ImageId
                                        AND i.AnnouncementId = @AnnouncementId
                                        AND a.UserId = @UserId;
                                      """;

        const string deleteImageSql = """
                                      DELETE FROM dbo.Images
                                      WHERE Id = @ImageId
                                        AND AnnouncementId = @AnnouncementId;
                                      """;

        const string selectNewPrimarySql = """
                                           ;WITH NextImage AS
                                           (
                                               SELECT TOP (1)
                                                   Id
                                               FROM dbo.Images
                                               WHERE AnnouncementId = @AnnouncementId
                                               ORDER BY
                                                   SortOrder,
                                                   Id
                                           )
                                           UPDATE i
                                           SET IsPrimary = 1
                                           FROM dbo.Images AS i
                                           INNER JOIN NextImage AS nextImage
                                               ON nextImage.Id = i.Id;
                                           """;

        await using var connection = _connectionFactory.CreateConnection();

        await connection.OpenAsync();

        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            string? fileName = null;
            var wasPrimary = false;

            await using (var selectCommand = connection.CreateCommand())
            {
                selectCommand.Transaction = transaction;
                selectCommand.CommandText = selectImageSql;

                AddParameter(selectCommand, "@ImageId", imageId);

                AddParameter(selectCommand, "@AnnouncementId", announcementId);

                AddParameter(selectCommand, "@UserId", userId);

                await using var reader = await selectCommand.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    fileName = reader.GetString(reader.GetOrdinal("FileName"));

                    wasPrimary = reader.GetBoolean(reader.GetOrdinal("IsPrimary"));
                }
            }

            if (fileName is null)
            {
                await transaction.CommitAsync();
                return null;
            }

            await using (var deleteCommand = connection.CreateCommand())
            {
                deleteCommand.Transaction = transaction;
                deleteCommand.CommandText = deleteImageSql;

                AddParameter(deleteCommand, "@ImageId", imageId);

                AddParameter(deleteCommand, "@AnnouncementId", announcementId);

                var affectedRows = await deleteCommand.ExecuteNonQueryAsync();

                if (affectedRows != 1)
                {
                    throw new InvalidOperationException("Billedet kunne ikke fjernes.");
                }
            }

            if (wasPrimary)
            {
                await using var primaryCommand = connection.CreateCommand();

                primaryCommand.Transaction = transaction;
                primaryCommand.CommandText = selectNewPrimarySql;

                AddParameter(primaryCommand, "@AnnouncementId", announcementId);

                await primaryCommand.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();

            return fileName;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static async Task<bool> OwnsAnnouncementAsync(DbConnection connection, DbTransaction transaction, string sql, int announcementId, int userId)
    {
        await using var command = connection.CreateCommand();

        command.Transaction = transaction;
        command.CommandText = sql;

        AddParameter(command, "@AnnouncementId", announcementId);

        AddParameter(command, "@UserId", userId);

        var result = await command.ExecuteScalarAsync();

        return result is not null && result is not DBNull;
    }

    private static async Task<int> GetImageCountAsync(DbConnection connection, DbTransaction transaction, string sql, int announcementId)
    {
        await using var command = connection.CreateCommand();

        command.Transaction = transaction;
        command.CommandText = sql;

        AddParameter(command, "@AnnouncementId", announcementId);

        var result = await command.ExecuteScalarAsync();

        return Convert.ToInt32(result);
    }

    private static async Task<AnnouncementImage> InsertImageAsync(DbConnection connection, DbTransaction transaction, string sql, int announcementId, string fileName, bool isPrimary, int sortOrder)
    {
        await using var command = connection.CreateCommand();

        command.Transaction = transaction;
        command.CommandText = sql;

        AddParameter(command, "@AnnouncementId", announcementId);

        AddParameter(command, "@FileName", fileName);

        AddParameter(command, "@IsPrimary", isPrimary);

        AddParameter(command, "@SortOrder", sortOrder);

        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            throw new InvalidOperationException("Billedmetadata kunne ikke oprettes.");
        }

        return ReadImage(reader);
    }

    public async Task<IReadOnlyList<AnnouncementImage>> GetForAdministrationAsync(int announcementId)
    {
        const string sql = """
                           SELECT
                               image.Id,
                               image.AnnouncementId,
                               image.FileName,
                               image.IsPrimary,
                               image.SortOrder,
                               image.CreatedAt
                           FROM dbo.Images AS image
                           INNER JOIN dbo.Announcement AS announcement
                               ON announcement.Id =
                                  image.AnnouncementId
                           WHERE announcement.Id =
                                 @AnnouncementId
                           ORDER BY
                               image.IsPrimary DESC,
                               image.SortOrder,
                               image.Id;
                           """;

        await using var connection = _connectionFactory.CreateConnection();

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();

        command.CommandText = sql;

        AddParameter(command, "@AnnouncementId", announcementId);

        var images = new List<AnnouncementImage>();

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            images.Add(ReadImage(reader));
        }

        return images;
    }

    public async Task<string?> DeleteForAdministrationWithAuditAsync(int announcementId, int imageId, int administratorUserId, Guid correlationId)
    {
        const string administratorSql = """
                                        SELECT CASE
                                            WHEN EXISTS
                                            (
                                                SELECT 1
                                                FROM dbo.Users AS admin
                                                INNER JOIN dbo.UserGroups AS userGroup
                                                    ON userGroup.Id =
                                                       admin.UserGroupId
                                                WHERE admin.Id =
                                                      @AdministratorUserId
                                                  AND admin.IsActive = 1
                                                  AND userGroup.Name =
                                                      N'Administrator'
                                            )
                                            THEN CAST(1 AS bit)
                                            ELSE CAST(0 AS bit)
                                        END;
                                        """;

        const string selectImageSql = """
                                      SELECT TOP (1)
                                          image.FileName,
                                          image.IsPrimary
                                      FROM dbo.Images AS image
                                          WITH (UPDLOCK, HOLDLOCK)
                                      INNER JOIN dbo.Announcement AS announcement
                                          ON announcement.Id =
                                             image.AnnouncementId
                                      WHERE image.Id = @ImageId
                                        AND image.AnnouncementId =
                                            @AnnouncementId;
                                      """;

        const string deleteImageSql = """
                                      DELETE FROM dbo.Images
                                      WHERE Id = @ImageId
                                        AND AnnouncementId =
                                            @AnnouncementId;
                                      """;

        const string selectNewPrimarySql = """
                                           ;WITH NextImage AS
                                           (
                                               SELECT TOP (1)
                                                   Id
                                               FROM dbo.Images
                                               WHERE AnnouncementId =
                                                     @AnnouncementId
                                               ORDER BY
                                                   SortOrder,
                                                   Id
                                           )
                                           UPDATE image
                                           SET IsPrimary = 1
                                           FROM dbo.Images AS image
                                           INNER JOIN NextImage AS nextImage
                                               ON nextImage.Id = image.Id;
                                           """;

        const string auditSql = """
                                INSERT INTO dbo.AuditLogs
                                (
                                    UserId,
                                    Action,
                                    EntityType,
                                    EntityId,
                                    Details,
                                    CorrelationId
                                )
                                VALUES
                                (
                                    @AdministratorUserId,
                                    @Action,
                                    @EntityType,
                                    @EntityId,
                                    @Details,
                                    @CorrelationId
                                );
                                """;

        await using var connection = _connectionFactory.CreateConnection();

        await connection.OpenAsync();

        // Serializable samt UPDLOCK/HOLDLOCK forhindrer,
        // at billedrækken ændres af en anden handling,
        // mens den slettes og et nyt primærbillede vælges.
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            // Administratorrollen kontrolleres igen direkte
            // i databasen.
            await using (var administratorCommand = connection.CreateCommand())
            {
                administratorCommand.Transaction = transaction;

                administratorCommand.CommandText = administratorSql;

                AddParameter(administratorCommand, "@AdministratorUserId", administratorUserId);

                var administratorResult = await administratorCommand.ExecuteScalarAsync();

                if (!Convert.ToBoolean(administratorResult))
                {
                    await transaction.RollbackAsync();

                    return null;
                }
            }

            string? fileName = null;
            var wasPrimary = false;

            // Billedet hentes og låses. Filnavnet skal
            // returneres til service-laget, så den fysiske
            // fil kan fjernes efter commit.
            await using (var selectCommand = connection.CreateCommand())
            {
                selectCommand.Transaction = transaction;
                selectCommand.CommandText = selectImageSql;

                AddParameter(selectCommand, "@ImageId", imageId);

                AddParameter(selectCommand, "@AnnouncementId", announcementId);

                await using var reader = await selectCommand.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    fileName = reader.GetString(reader.GetOrdinal("FileName"));

                    wasPrimary = reader.GetBoolean(reader.GetOrdinal("IsPrimary"));
                }
            }

            if (fileName is null)
            {
                await transaction.RollbackAsync();

                return null;
            }

            // Slet billedets metadata.
            await using (var deleteCommand = connection.CreateCommand())
            {
                deleteCommand.Transaction = transaction;
                deleteCommand.CommandText = deleteImageSql;

                AddParameter(deleteCommand, "@ImageId", imageId);

                AddParameter(deleteCommand, "@AnnouncementId", announcementId);

                var deletedImages = await deleteCommand.ExecuteNonQueryAsync();

                if (deletedImages != 1)
                {
                    throw new InvalidOperationException("Billedet kunne ikke fjernes.");
                }
            }

            // Hvis primærbilledet blev slettet, vælges det
            // næste billede efter SortOrder og Id.
            if (wasPrimary)
            {
                await using var primaryCommand = connection.CreateCommand();

                primaryCommand.Transaction = transaction;

                primaryCommand.CommandText = selectNewPrimarySql;

                AddParameter(primaryCommand, "@AnnouncementId", announcementId);

                await primaryCommand.ExecuteNonQueryAsync();
            }

            // Metadataændringen og auditloggen gemmes samlet.
            await using (var auditCommand = connection.CreateCommand())
            {
                auditCommand.Transaction = transaction;
                auditCommand.CommandText = auditSql;

                AddParameter(auditCommand, "@AdministratorUserId", administratorUserId);

                AddParameter(auditCommand, "@Action", "AnnouncementImageDeleted");

                AddParameter(auditCommand, "@EntityType", "AnnouncementImage");

                AddParameter(auditCommand, "@EntityId", imageId.ToString());

                AddParameter(auditCommand, "@Details", $"Billede fjernet fra annonce " + $"{announcementId} af administrator.");

                AddParameter(auditCommand, "@CorrelationId", correlationId);

                var insertedAuditLogs = await auditCommand.ExecuteNonQueryAsync();

                if (insertedAuditLogs != 1)
                {
                    throw new InvalidOperationException("Auditloggen kunne ikke gemmes.");
                }
            }

            await transaction.CommitAsync();

            return fileName;
        }
        catch
        {
            await transaction.RollbackAsync();

            throw;
        }
    }

    private static AnnouncementImage ReadImage(DbDataReader reader)
    {
        return new AnnouncementImage
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),

            AnnouncementId = reader.GetInt32(reader.GetOrdinal("AnnouncementId")),

            FileName = reader.GetString(reader.GetOrdinal("FileName")),

            IsPrimary = reader.GetBoolean(reader.GetOrdinal("IsPrimary")),

            SortOrder = reader.GetInt32(reader.GetOrdinal("SortOrder")),

            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }

    private static void AddParameter(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();

        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;

        command.Parameters.Add(parameter);
    }
}