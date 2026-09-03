using System.Data.Common;
using BrugtBilsHandelsPortal.DTOs.Comments;

namespace BrugtBilsHandelsPortal.Data.Mapping;

public static class CommentMapper
{
    public static CommentDto MapToDto(DbDataReader reader)
    {
        var parentCommentIdOrdinal = reader.GetOrdinal("ParentCommentId");

        return new CommentDto
        {
            Id = reader.GetInt64(reader.GetOrdinal("Id")),

            AnnouncementId = reader.GetInt32(reader.GetOrdinal("AnnouncementId")),

            UserId = reader.GetInt32(reader.GetOrdinal("UserId")),

            Username = reader.GetString(reader.GetOrdinal("Username")),

            Text = reader.GetString(reader.GetOrdinal("Comment")),

            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),

            ParentCommentId = reader.IsDBNull(parentCommentIdOrdinal) ? null : reader.GetInt64(parentCommentIdOrdinal),

            IsDeleted = reader.GetBoolean(reader.GetOrdinal("IsDeleted"))
        };
    }

    public static IReadOnlyList<CommentDto> BuildTree(IEnumerable<CommentDto> comments)
    {
        var materializedComments = comments.ToList();

        var repliesByParent = materializedComments.Where(comment => comment.ParentCommentId.HasValue).GroupBy(comment => comment.ParentCommentId!.Value).ToDictionary(group => group.Key, group => (IReadOnlyList<CommentDto>)group.OrderBy(comment => comment.CreatedAt).ToList());

        return materializedComments.Where(comment => comment.ParentCommentId is null).OrderBy(comment => comment.CreatedAt).Select(comment => CopyWithReplies(comment, repliesByParent)).ToList();
    }

    private static CommentDto CopyWithReplies(CommentDto comment, IReadOnlyDictionary<long, IReadOnlyList<CommentDto>> repliesByParent)
    {
        var replies = repliesByParent.TryGetValue(comment.Id, out var directReplies) ? directReplies.Select(reply => CopyWithReplies(reply, repliesByParent)).ToList() : [];

        return new CommentDto
        {
            Id = comment.Id,
            AnnouncementId = comment.AnnouncementId,
            UserId = comment.UserId,
            Username = comment.Username,
            Text = comment.Text,
            CreatedAt = comment.CreatedAt,
            ParentCommentId = comment.ParentCommentId,
            IsDeleted = comment.IsDeleted,
            Replies = replies
        };
    }
}