namespace BrugtBilsHandelsPortal.DTOs.Messages;

public class MessageDto
{
    public long Id { get; init; }

    public int AnnouncementId { get; init; }

    public string AnnouncementTitle { get; init; }
        = string.Empty;

    public int SenderUserId { get; init; }

    public string SenderUsername { get; init; }
        = string.Empty;

    public int ReceiverUserId { get; init; }

    public string ReceiverUsername { get; init; }
        = string.Empty;

    public string Text { get; init; }
        = string.Empty;

    public DateTime CreatedAt { get; init; }

    public bool IsRead { get; init; }

    public DateTime? ReadAt { get; init; }
}