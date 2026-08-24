using System.Data.Common;
using BrugtBilsHandelsPortal.DTOs.Messages;

namespace BrugtBilsHandelsPortal.Data.Mapping;

public static class MessageMapper
{
    public static MessageDto MapToDto(
        DbDataReader reader)
    {
        var readAtOrdinal =
            reader.GetOrdinal("ReadAt");

        return new MessageDto
        {
            Id = reader.GetInt64(
                reader.GetOrdinal("Id")),

            AnnouncementId = reader.GetInt32(
                reader.GetOrdinal("AnnouncementId")),

            AnnouncementTitle = reader.GetString(
                reader.GetOrdinal("AnnouncementTitle")),

            SenderUserId = reader.GetInt32(
                reader.GetOrdinal("SenderUserId")),

            SenderUsername = reader.GetString(
                reader.GetOrdinal("SenderUsername")),

            ReceiverUserId = reader.GetInt32(
                reader.GetOrdinal("ReceiverUserId")),

            ReceiverUsername = reader.GetString(
                reader.GetOrdinal("ReceiverUsername")),

            Text = reader.GetString(
                reader.GetOrdinal("Message")),

            CreatedAt = reader.GetDateTime(
                reader.GetOrdinal("CreatedAt")),

            IsRead = reader.GetBoolean(
                reader.GetOrdinal("IsRead")),

            ReadAt = reader.IsDBNull(readAtOrdinal)
                ? null
                : reader.GetDateTime(readAtOrdinal)
        };
    }
}