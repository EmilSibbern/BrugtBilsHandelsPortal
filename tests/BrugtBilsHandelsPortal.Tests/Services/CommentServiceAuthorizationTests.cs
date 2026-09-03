using System.ComponentModel.DataAnnotations;
using BrugtBilsHandelsPortal.Data.Repositories.Interfaces;
using BrugtBilsHandelsPortal.DTOs.Announcements;
using BrugtBilsHandelsPortal.DTOs.Comments;
using BrugtBilsHandelsPortal.Models.Entities;
using BrugtBilsHandelsPortal.Services.Implementations;
using BrugtBilsHandelsPortal.Tests.Fakes;
using Xunit;

namespace BrugtBilsHandelsPortal.Tests.Services;

public class CommentServiceAuthorizationTests
{
    [Fact]
    [Trait("UserStory", "US14")]
    public async Task GetForAnnouncementAsync_AsGuest_ReturnsPublicComments()
    {
        var commentRepository = new FakeCommentRepository();
        commentRepository.PublicComments.Add(CreateCommentDto(id: 4, userId: 8));

        var service = CreateService(commentRepository, currentUserId: null);

        var comments = await service.GetForAnnouncementAsync(42);

        var comment = Assert.Single(comments);
        Assert.Equal("Er bilen stadig til salg?", comment.Text);
        Assert.Equal(1, commentRepository.GetForAnnouncementCallCount);
        Assert.Equal(42, commentRepository.LastAnnouncementId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [Trait("UserStory", "US14")]
    public async Task GetForAnnouncementAsync_WithInvalidId_DoesNotQueryRepository(int announcementId)
    {
        var commentRepository = new FakeCommentRepository();
        var service = CreateService(commentRepository, currentUserId: null);

        var comments = await service.GetForAnnouncementAsync(announcementId);

        Assert.Empty(comments);
        Assert.Equal(0, commentRepository.GetForAnnouncementCallCount);
    }

    [Fact]
    [Trait("UserStory", "US14")]
    public async Task CreateAsync_AsGuest_IsRejectedBeforeDatabaseWrite()
    {
        var commentRepository = new FakeCommentRepository();
        var service = CreateService(commentRepository, currentUserId: null);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateAsync(CreateRequest()));

        Assert.Equal(0, commentRepository.CreateCallCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [Trait("UserStory", "US14")]
    public async Task CreateAsync_WithBlankText_IsRejected(string text)
    {
        var commentRepository = new FakeCommentRepository();
        var service = CreateService(commentRepository, currentUserId: 9);

        var request = CreateRequest(text: text);

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(request));

        Assert.Equal(0, commentRepository.CreateCallCount);
    }

    [Fact]
    [Trait("UserStory", "US14")]
    public async Task CreateAsync_SetsUserIdServerSideAndTrimsText()
    {
        var commentRepository = new FakeCommentRepository
        {
            CreatedCommentId = 17
        };

        commentRepository.CommentsById[17] = CreateCommentDto(id: 17, userId: 9);

        var service = CreateService(commentRepository, currentUserId: 9);

        var request = CreateRequest(text: "  Er bilen stadig til salg?  ");

        var result = await service.CreateAsync(request);

        Assert.Equal(17, result.Id);
        Assert.NotNull(commentRepository.CreatedComment);
        Assert.Equal(9, commentRepository.CreatedComment.UserId);
        Assert.Equal(42, commentRepository.CreatedComment.AnnouncementId);
        Assert.Equal("Er bilen stadig til salg?", commentRepository.CreatedComment.Text);
        Assert.Equal(1, commentRepository.CreateCallCount);
    }

    [Fact]
    [Trait("UserStory", "US14")]
    public async Task CreateAsync_WithMoreThanFifteenHundredCharacters_IsRejected()
    {
        var commentRepository = new FakeCommentRepository();
        var service = CreateService(commentRepository, currentUserId: 9);

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(CreateRequest(text: new string('a', 1501))));

        Assert.Equal(0, commentRepository.CreateCallCount);
    }

    [Fact]
    [Trait("UserStory", "US14")]
    public async Task CreateAsync_ForInactiveAnnouncement_IsRejected()
    {
        var commentRepository = new FakeCommentRepository();
        var service = CreateService(commentRepository, currentUserId: 9, announcementIsActive: false);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(CreateRequest()));

        Assert.Equal(0, commentRepository.CountCreatedSinceCallCount);
        Assert.Equal(0, commentRepository.CreateCallCount);
    }

    [Fact]
    [Trait("UserStory", "US14")]
    public async Task CreateAsync_WhenRateLimitIsReached_IsRejected()
    {
        var commentRepository = new FakeCommentRepository
        {
            CreatedSinceCount = 5
        };

        var service = CreateService(commentRepository, currentUserId: 9);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(CreateRequest()));

        Assert.Equal(1, commentRepository.CountCreatedSinceCallCount);
        Assert.Equal(9, commentRepository.LastCountedUserId);
        Assert.Equal(0, commentRepository.CreateCallCount);
    }

    [Fact]
    [Trait("UserStory", "US15")]
    public async Task CreateReply_ForCommentOnDifferentAnnouncement_IsRejected()
    {
        var commentRepository = new FakeCommentRepository();
        commentRepository.CommentsById[5] = new CommentDto
        {
            Id = 5,
            AnnouncementId = 99,
            UserId = 8,
            Username = "anden-bruger",
            Text = "Hovedkommentar"
        };

        var service = CreateService(commentRepository, currentUserId: 9);

        var request = CreateRequest(parentCommentId: 5);

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(request));

        Assert.Equal(0, commentRepository.CreateCallCount);
    }

    [Fact]
    [Trait("UserStory", "US15")]
    public async Task CreateReply_ForMainComment_SetsParentCommentId()
    {
        var commentRepository = new FakeCommentRepository
        {
            CreatedCommentId = 17
        };
        commentRepository.CommentsById[5] = CreateCommentDto(id: 5, userId: 8);
        commentRepository.CommentsById[17] = new CommentDto
        {
            Id = 17,
            AnnouncementId = 42,
            UserId = 9,
            Username = "køber",
            Text = "Ja, den er stadig til salg.",
            ParentCommentId = 5
        };

        var service = CreateService(commentRepository, currentUserId: 9);

        var result = await service.CreateAsync(CreateRequest(text: "Ja, den er stadig til salg.", parentCommentId: 5));

        Assert.NotNull(commentRepository.CreatedComment);
        Assert.Equal(5, commentRepository.CreatedComment.ParentCommentId);
        Assert.Equal(5, result.ParentCommentId);
    }

    [Fact]
    [Trait("UserStory", "US15")]
    public async Task CreateReply_ForReply_IsRejectedBeforeDatabaseWrite()
    {
        var commentRepository = new FakeCommentRepository();
        commentRepository.CommentsById[6] = new CommentDto
        {
            Id = 6,
            AnnouncementId = 42,
            UserId = 8,
            Username = "anden-bruger",
            Text = "Et eksisterende svar",
            ParentCommentId = 5
        };

        var service = CreateService(commentRepository, currentUserId: 9);

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(CreateRequest(parentCommentId: 6)));

        Assert.Equal(0, commentRepository.CountCreatedSinceCallCount);
        Assert.Equal(0, commentRepository.CreateCallCount);
    }

    [Fact]
    public async Task DeleteAsync_AsDifferentUser_IsRejected()
    {
        var commentRepository = new FakeCommentRepository();
        commentRepository.CommentsById[5] = CreateCommentDto(id: 5, userId: 8);

        var service = CreateService(commentRepository, currentUserId: 9);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.DeleteAsync(5));

        Assert.Equal(0, commentRepository.SoftDeleteCallCount);
    }

    [Fact]
    public async Task DeleteAsync_AsOwner_SoftDeletesComment()
    {
        var commentRepository = new FakeCommentRepository();
        commentRepository.CommentsById[5] = CreateCommentDto(id: 5, userId: 9);

        var service = CreateService(commentRepository, currentUserId: 9);

        await service.DeleteAsync(5);

        Assert.Equal(1, commentRepository.SoftDeleteCallCount);
        Assert.Equal(5, commentRepository.LastDeletedCommentId);
        Assert.Equal(9, commentRepository.LastDeletedByUserId);
    }

    [Fact]
    public async Task DeleteAsync_AsAdministrator_CanDeleteOtherUsersComment()
    {
        var commentRepository = new FakeCommentRepository();
        commentRepository.CommentsById[5] = CreateCommentDto(id: 5, userId: 8);

        var service = CreateService(commentRepository, currentUserId: 1, isAdministrator: true);

        await service.DeleteAsync(5);

        Assert.Equal(1, commentRepository.SoftDeleteCallCount);
        Assert.Equal(1, commentRepository.LastDeletedByUserId);
    }

    private static CommentService CreateService(FakeCommentRepository commentRepository, int? currentUserId, bool isAdministrator = false, bool announcementIsActive = true)
    {
        var announcementRepository = new FakeAnnouncementRepository();

        announcementRepository.Details[42] = new AnnouncementDetailsDto
            {
                Id = 42,
                IsActive = announcementIsActive,
                SellerUserId = 7,
                SellerUsername = "sælger"
            };

        return new CommentService(commentRepository, announcementRepository, new FakeCurrentUserService { UserId = currentUserId, IsAdministrator = isAdministrator });
    }

    private static CreateCommentDto CreateRequest(string text = "Er bilen stadig til salg?", long? parentCommentId = null)
    {
        return new CreateCommentDto
        {
            AnnouncementId = 42,
            Text = text,
            ParentCommentId = parentCommentId
        };
    }

    private static CommentDto CreateCommentDto(long id, int userId)
    {
        return new CommentDto
        {
            Id = id,
            AnnouncementId = 42,
            UserId = userId,
            Username = "køber",
            Text = "Er bilen stadig til salg?",
            CreatedAt = new DateTime(2026, 8, 23)
        };
    }

    private sealed class FakeCommentRepository : ICommentRepository
    {
        public List<CommentDto> PublicComments { get; } = [];
        public Dictionary<long, CommentDto> CommentsById { get; } = [];

        public long CreatedCommentId { get; set; } = 1;
        public Comment? CreatedComment { get; private set; }
        public int CreateCallCount { get; private set; }
        public int CreatedSinceCount { get; set; }
        public int CountCreatedSinceCallCount { get; private set; }
        public int GetForAnnouncementCallCount { get; private set; }
        public int SoftDeleteCallCount { get; private set; }
        public int? LastAnnouncementId { get; private set; }
        public long? LastDeletedCommentId { get; private set; }
        public int? LastDeletedByUserId { get; private set; }
        public int? LastCountedUserId { get; private set; }

        public Task<IReadOnlyList<CommentDto>> GetForActiveAnnouncementAsync(int announcementId)
        {
            GetForAnnouncementCallCount++;
            LastAnnouncementId = announcementId;
            return Task.FromResult<IReadOnlyList<CommentDto>>(PublicComments);
        }

        public Task<CommentDto?> GetByIdAsync(long commentId)
        {
            CommentsById.TryGetValue(commentId, out var comment);
            return Task.FromResult(comment);
        }

        public Task<long> CreateAsync(Comment comment)
        {
            CreateCallCount++;
            CreatedComment = comment;
            return Task.FromResult(CreatedCommentId);
        }

        public Task<int> CountCreatedSinceAsync(int userId, DateTime since)
        {
            CountCreatedSinceCallCount++;
            LastCountedUserId = userId;
            return Task.FromResult(CreatedSinceCount);
        }

        public Task<bool> SoftDeleteAsync(long commentId, int deletedByUserId, DateTime deletedAt)
        {
            SoftDeleteCallCount++;
            LastDeletedCommentId = commentId;
            LastDeletedByUserId = deletedByUserId;
            return Task.FromResult(true);
        }
    }
}