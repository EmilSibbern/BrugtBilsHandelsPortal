using System.Data.Common;
using BrugtBilsHandelsPortal.Data.Connections;
using BrugtBilsHandelsPortal.Data.Mapping;
using BrugtBilsHandelsPortal.Data.Repositories.Interfaces;
using BrugtBilsHandelsPortal.DTOs.Comments;
using BrugtBilsHandelsPortal.Models.Entities;

namespace BrugtBilsHandelsPortal.Data.Repositories.Implementations;

/// <summary>Gemmer kommentartråde og bevarer historik gennem soft delete.</summary>
public class CommentRepository : ICommentRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public CommentRepository(
        IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<CommentDto>>
        GetForActiveAnnouncementAsync(
            int announcementId)
    {
        const string sql = """
                           SELECT
                               c.Id,
                               c.AnnouncementId,
                               c.UserId,
                               u.Username,
                               CASE
                                   WHEN c.IsDeleted = 1
                                       THEN N'Kommentaren er fjernet'
                                   ELSE c.Comment
                               END AS Comment,
                               c.CreatedAt,
                               c.ParentCommentId,
                               c.IsDeleted
                           FROM dbo.Comments AS c
                           INNER JOIN dbo.Users AS u
                               ON u.Id = c.UserId
                           INNER JOIN dbo.Announcement AS a
                               ON a.Id = c.AnnouncementId
                           INNER JOIN dbo.Users AS seller
                               ON seller.Id = a.UserId
                           WHERE c.AnnouncementId = @AnnouncementId
                             AND a.IsActive = 1
                             AND seller.IsActive = 1
                           ORDER BY
                               c.CreatedAt,
                               c.Id;
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

        await using var reader =
            await command.ExecuteReaderAsync();

        var comments = new List<CommentDto>();

        while (await reader.ReadAsync())
        {
            comments.Add(CommentMapper.MapToDto(reader));
        }

        return CommentMapper.BuildTree(comments);
    }

    public async Task<CommentDto?> GetByIdAsync(
        long commentId)
    {
        const string sql = """
                           SELECT TOP (1)
                               c.Id,
                               c.AnnouncementId,
                               c.UserId,
                               u.Username,
                               CASE
                                   WHEN c.IsDeleted = 1
                                       THEN N'Kommentaren er fjernet'
                                   ELSE c.Comment
                               END AS Comment,
                               c.CreatedAt,
                               c.ParentCommentId,
                               c.IsDeleted
                           FROM dbo.Comments AS c
                           INNER JOIN dbo.Users AS u
                               ON u.Id = c.UserId
                           WHERE c.Id = @CommentId;
                           """;

        await using var connection =
            _connectionFactory.CreateConnection();

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        AddParameter(
            command,
            "@CommentId",
            commentId);

        await using var reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return CommentMapper.MapToDto(reader);
    }

    public async Task<long> CreateAsync(
        Comment comment)
    {
        const string sql = """
                           SET NOCOUNT ON;

                           DECLARE @InsertedComments TABLE
                           (
                               Id bigint NOT NULL
                           );

                           INSERT INTO dbo.Comments
                           (
                               AnnouncementId,
                               UserId,
                               Comment,
                               ParentCommentId
                           )
                           OUTPUT INSERTED.Id
                               INTO @InsertedComments (Id)
                           VALUES
                           (
                               @AnnouncementId,
                               @UserId,
                               @Comment,
                               @ParentCommentId
                           );

                           SELECT Id
                           FROM @InsertedComments;
                           """;

        await using var connection =
            _connectionFactory.CreateConnection();

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        AddParameter(
            command,
            "@AnnouncementId",
            comment.AnnouncementId);

        AddParameter(
            command,
            "@UserId",
            comment.UserId);

        AddParameter(
            command,
            "@Comment",
            comment.Text);

        AddParameter(
            command,
            "@ParentCommentId",
            comment.ParentCommentId);

        var result = await command.ExecuteScalarAsync();

        if (result is null || result is DBNull)
        {
            throw new InvalidOperationException(
                "Kommentaren kunne ikke oprettes.");
        }

        return Convert.ToInt64(result);
    }

    public async Task<bool> SoftDeleteAsync(
        long commentId,
        int deletedByUserId,
        DateTime deletedAt)
    {
        const string sql = """
                           UPDATE dbo.Comments
                           SET
                               IsDeleted = 1,
                               DeletedAt = @DeletedAt,
                               DeletedByUserId = @DeletedByUserId
                           WHERE Id = @CommentId
                             AND IsDeleted = 0;
                           """;

        await using var connection =
            _connectionFactory.CreateConnection();

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        AddParameter(
            command,
            "@CommentId",
            commentId);

        AddParameter(
            command,
            "@DeletedByUserId",
            deletedByUserId);

        AddParameter(
            command,
            "@DeletedAt",
            deletedAt);

        var affectedRows =
            await command.ExecuteNonQueryAsync();

        return affectedRows == 1;
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
}
