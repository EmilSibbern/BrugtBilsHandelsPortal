using BrugtBilsHandelsPortal.DTOs.Comments;

namespace BrugtBilsHandelsPortal.Services.Interfaces;

/// <summary>Håndhæver forretnings- og adgangsregler for kommentarer på annoncer.</summary>
public interface ICommentService
{
    /// <summary>Henter synlige kommentarer til en aktiv annonce.</summary>
    Task<IReadOnlyList<CommentDto>> GetForAnnouncementAsync(int announcementId);

    /// <summary>Opretter en kommentar eller et svar som den aktuelle bruger.</summary>
    Task<CommentDto> CreateAsync(CreateCommentDto request);

    /// <summary>Soft-sletter en kommentar, når brugeren er forfatter eller administrator.</summary>
    Task DeleteAsync(long commentId);
}