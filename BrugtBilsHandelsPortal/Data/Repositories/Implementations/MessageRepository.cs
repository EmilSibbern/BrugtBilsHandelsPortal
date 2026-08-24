using System.Data.Common;
using BrugtBilsHandelsPortal.Data.Connections;
using BrugtBilsHandelsPortal.Data.Mapping;
using BrugtBilsHandelsPortal.Data.Repositories.Interfaces;
using BrugtBilsHandelsPortal.DTOs.Messages;
using BrugtBilsHandelsPortal.Models.Entities;

namespace BrugtBilsHandelsPortal.Data.Repositories.Implementations;

public class MessageRepository : IMessageRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public MessageRepository(
        IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<long> CreateAsync(
        Message message)
    {
        const string sql = """
                           SET NOCOUNT ON;

                           DECLARE @InsertedMessages TABLE
                           (
                               Id bigint NOT NULL
                           );

                           INSERT INTO dbo.Messages
                           (
                               AnnouncementId,
                               SenderUserId,
                               ReceiverUserId,
                               Message
                           )
                           OUTPUT INSERTED.Id
                               INTO @InsertedMessages (Id)
                           VALUES
                           (
                               @AnnouncementId,
                               @SenderUserId,
                               @ReceiverUserId,
                               @Message
                           );

                           SELECT Id
                           FROM @InsertedMessages;
                           """;

        await using var connection =
            _connectionFactory.CreateConnection();

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        AddParameter(
            command,
            "@AnnouncementId",
            message.AnnouncementId);

        AddParameter(
            command,
            "@SenderUserId",
            message.SenderUserId);

        AddParameter(
            command,
            "@ReceiverUserId",
            message.ReceiverUserId);

        AddParameter(
            command,
            "@Message",
            message.Text);

        var result = await command.ExecuteScalarAsync();

        if (result is null || result is DBNull)
        {
            throw new InvalidOperationException(
                "Beskeden kunne ikke oprettes.");
        }

        return Convert.ToInt64(result);
    }

    public async Task<MessageDto?>
        GetByIdForParticipantAsync(
            long messageId,
            int currentUserId)
    {
        const string sql = """
                           SELECT TOP (1)
                               m.Id,
                               m.AnnouncementId,
                               a.Title AS AnnouncementTitle,
                               m.SenderUserId,
                               sender.Username AS SenderUsername,
                               m.ReceiverUserId,
                               receiver.Username AS ReceiverUsername,
                               m.Message,
                               m.CreatedAt,
                               m.IsRead,
                               m.ReadAt
                           FROM dbo.Messages AS m
                           INNER JOIN dbo.Announcement AS a
                               ON a.Id = m.AnnouncementId
                           INNER JOIN dbo.Users AS sender
                               ON sender.Id = m.SenderUserId
                           INNER JOIN dbo.Users AS receiver
                               ON receiver.Id = m.ReceiverUserId
                           WHERE m.Id = @MessageId
                             AND
                             (
                                 m.SenderUserId = @CurrentUserId
                                 OR m.ReceiverUserId = @CurrentUserId
                             );
                           """;

        await using var connection =
            _connectionFactory.CreateConnection();

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        AddParameter(
            command,
            "@MessageId",
            messageId);

        AddParameter(
            command,
            "@CurrentUserId",
            currentUserId);

        await using var reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return MessageMapper.MapToDto(reader);
    }

    public async Task<IReadOnlyList<MessageDto>>
        GetInboxAsync(
            int receiverUserId)
    {
        const string sql = """
                           SELECT
                               m.Id,
                               m.AnnouncementId,
                               a.Title AS AnnouncementTitle,
                               m.SenderUserId,
                               sender.Username AS SenderUsername,
                               m.ReceiverUserId,
                               receiver.Username AS ReceiverUsername,
                               m.Message,
                               m.CreatedAt,
                               m.IsRead,
                               m.ReadAt
                           FROM dbo.Messages AS m
                           INNER JOIN dbo.Announcement AS a
                               ON a.Id = m.AnnouncementId
                           INNER JOIN dbo.Users AS sender
                               ON sender.Id = m.SenderUserId
                           INNER JOIN dbo.Users AS receiver
                               ON receiver.Id = m.ReceiverUserId
                           WHERE m.ReceiverUserId = @ReceiverUserId
                           ORDER BY
                               m.CreatedAt DESC,
                               m.Id DESC;
                           """;

        await using var connection =
            _connectionFactory.CreateConnection();

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        AddParameter(
            command,
            "@ReceiverUserId",
            receiverUserId);

        await using var reader =
            await command.ExecuteReaderAsync();

        var messages = new List<MessageDto>();

        while (await reader.ReadAsync())
        {
            messages.Add(MessageMapper.MapToDto(reader));
        }

        return messages;
    }

    public async Task<int> CountSentSinceAsync(
        int senderUserId,
        DateTime since)
    {
        const string sql = """
                           SELECT COUNT(*)
                           FROM dbo.Messages
                           WHERE SenderUserId = @SenderUserId
                             AND CreatedAt >= @Since;
                           """;

        await using var connection =
            _connectionFactory.CreateConnection();

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        AddParameter(
            command,
            "@SenderUserId",
            senderUserId);

        AddParameter(
            command,
            "@Since",
            since);

        var result = await command.ExecuteScalarAsync();

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
}