using System.ComponentModel.DataAnnotations;
using BrugtBilsHandelsPortal.Data.Repositories.Interfaces;
using BrugtBilsHandelsPortal.DTOs.Comments;
using BrugtBilsHandelsPortal.Models.Entities;
using BrugtBilsHandelsPortal.Security;
using BrugtBilsHandelsPortal.Services.Interfaces;

namespace BrugtBilsHandelsPortal.Services.Implementations;

/// <summary>Håndterer kommentartråde, ejerskab og administrativ moderation.</summary>
public class CommentService : ICommentService
{

    private const int MaxCommentsPerMinute = 5;

    private readonly ICommentRepository _commentRepository;
    private readonly IAnnouncementRepository _announcementRepository;
    private readonly ICurrentUserService _currentUserService;

    public CommentService(
        ICommentRepository commentRepository,
        IAnnouncementRepository announcementRepository,
        ICurrentUserService currentUserService)
    {
        _commentRepository = commentRepository;
        _announcementRepository = announcementRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<CommentDto>>
        GetForAnnouncementAsync(
            int announcementId)
    {
        if (announcementId <= 0)
        {
            return Array.Empty<CommentDto>();
        }

        return await _commentRepository
            .GetForActiveAnnouncementAsync(
                announcementId);
    }

    public async Task<CommentDto> CreateAsync(
        CreateCommentDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateRequest(request);

        var userId =
            await _currentUserService.GetUserIdAsync();

        if (userId is null)
        {
            throw new UnauthorizedAccessException(
                "Du skal være logget ind for at kommentere.");
        }

        var announcement =
            await _announcementRepository.GetActiveByIdAsync(
                request.AnnouncementId);

        if (announcement is null)
        {
            throw new InvalidOperationException(
                "Annoncen findes ikke eller er ikke længere aktiv.");
        }

        if (request.ParentCommentId.HasValue)
        {
            await ValidateParentCommentAsync(
                request.ParentCommentId.Value,
                request.AnnouncementId);
        }

        var rateLimitStart =
            DateTime.UtcNow.AddMinutes(-1);

        var recentlyCreatedCount =
            await _commentRepository.CountCreatedSinceAsync(
                userId.Value,
                rateLimitStart);

        if (recentlyCreatedCount >= MaxCommentsPerMinute)
        {
            throw new InvalidOperationException(
                "Du har skrevet for mange kommentarer. Vent et øjeblik og prøv igen.");
        }

        var comment = new Comment
        {
            AnnouncementId = request.AnnouncementId,
            UserId = userId.Value,
            Text = request.Text.Trim(),
            ParentCommentId = request.ParentCommentId
        };

        var commentId =
            await _commentRepository.CreateAsync(comment);

        var createdComment =
            await _commentRepository.GetByIdAsync(commentId);

        if (createdComment is null)
        {
            throw new InvalidOperationException(
                "Kommentaren blev oprettet, men kunne ikke genindlæses.");
        }

        return createdComment;
    }

    public async Task DeleteAsync(
        long commentId)
    {
        if (commentId <= 0)
        {
            throw new ValidationException(
                "Kommentaren er ugyldig.");
        }

        var userId =
            await _currentUserService.GetUserIdAsync();

        if (userId is null)
        {
            throw new UnauthorizedAccessException(
                "Du skal være logget ind for at fjerne kommentarer.");
        }

        var comment =
            await _commentRepository.GetByIdAsync(commentId);

        if (comment is null)
        {
            throw new InvalidOperationException(
                "Kommentaren findes ikke.");
        }

        if (comment.IsDeleted)
        {
            return;
        }

        var isAdministrator =
            await _currentUserService.IsInRoleAsync(
                "Administrator");

        var isOwner = comment.UserId == userId.Value;

        if (!isOwner && !isAdministrator)
        {
            throw new UnauthorizedAccessException(
                "Du har ikke adgang til at fjerne kommentaren.");
        }

        var deleted =
            await _commentRepository.SoftDeleteAsync(
                commentId,
                userId.Value,
                DateTime.UtcNow);

        if (!deleted)
        {
            throw new InvalidOperationException(
                "Kommentaren kunne ikke fjernes.");
        }
    }

    private static void ValidateRequest(
        CreateCommentDto request)
    {
        var validationResults =
            new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            validationResults,
            validateAllProperties: true);

        if (!isValid)
        {
            throw new ValidationException(
                validationResults.First().ErrorMessage
                ?? "Kommentaren er ugyldig.");
        }

        if (string.IsNullOrWhiteSpace(request.Text))
        {
            throw new ValidationException(
                "Kommentaren må ikke være tom.");
        }
    }

    private async Task ValidateParentCommentAsync(
        long parentCommentId,
        int announcementId)
    {
        if (parentCommentId <= 0)
        {
            throw new ValidationException(
                "Den valgte hovedkommentar er ugyldig.");
        }

        var parentComment =
            await _commentRepository.GetByIdAsync(
                parentCommentId);

        if (parentComment is null
            || parentComment.IsDeleted)
        {
            throw new InvalidOperationException(
                "Hovedkommentaren findes ikke.");
        }

        if (parentComment.AnnouncementId
            != announcementId)
        {
            throw new ValidationException(
                "Der kan kun svares på kommentarer fra samme annonce.");
        }

        if (parentComment.ParentCommentId.HasValue)
        {
            throw new ValidationException(
                "Der kan kun svares direkte på en hovedkommentar.");
        }
    }
}
