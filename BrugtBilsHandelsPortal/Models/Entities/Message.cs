namespace BrugtBilsHandelsPortal.Models.Entities;

public class Message
{
    public long Id { get; set; }

    public int AnnouncementId { get; set; }

    public int SenderUserId { get; set; }

    public int ReceiverUserId { get; set; }

    public string Text { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public bool IsRead { get; set; }

    public DateTime? ReadAt { get; set; }
}