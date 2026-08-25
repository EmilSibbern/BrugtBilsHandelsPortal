namespace BrugtBilsHandelsPortal.DTOs.Messages;

public class ConversationDto
{
    public int AnnouncementId { get; init; }

    public string AnnouncementTitle { get; init; }
        = string.Empty;

    public int OtherUserId { get; init; }

    public string OtherUsername { get; init; }
        = string.Empty;

    public IReadOnlyList<MessageDto> Messages { get; init; }
        = Array.Empty<MessageDto>();
}