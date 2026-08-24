using BrugtBilsHandelsPortal.Components.Shared;
using BrugtBilsHandelsPortal.DTOs.Comments;
using BrugtBilsHandelsPortal.Services.Interfaces;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BrugtBilsHandelsPortal.Tests.Components;

public class CommentComponentsTests
{
    [Fact]
    [Trait("UserStory", "US14")]
    public void CommentList_ShowsAuthorTextAndCreatedAt()
    {
        using var context = new BunitContext();
        var createdAt = new DateTime(
            2026, 8, 24, 10, 30, 0, DateTimeKind.Utc);

        var cut = context.Render<CommentList>(parameters =>
            parameters.Add(component => component.Comments,
            [
                CreateComment(createdAt: createdAt)
            ]));

        var comment = cut.Find("[data-testid='comment']");
        Assert.Contains("interesseret-køber", comment.TextContent);
        Assert.Contains("Er bilen stadig til salg?", comment.TextContent);
        Assert.Equal(
            createdAt.ToString("O"),
            comment.QuerySelector("time")?.GetAttribute("datetime"));
    }

    [Fact]
    [Trait("UserStory", "US14")]
    public void CommentList_WithNoComments_ShowsEmptyState()
    {
        using var context = new BunitContext();

        var cut = context.Render<CommentList>(parameters =>
            parameters.Add(
                component => component.Comments,
                Array.Empty<CommentDto>()));

        Assert.Contains(
            "Der er endnu ingen kommentarer.",
            cut.Find("[data-testid='comments-empty']").TextContent);
    }

    [Fact]
    [Trait("UserStory", "US14")]
    public void CommentList_OutputEncodesCommentText()
    {
        using var context = new BunitContext();
        const string unsafeText = "<script>alert('xss')</script>";

        var cut = context.Render<CommentList>(parameters =>
            parameters.Add(component => component.Comments,
            [
                CreateComment(text: unsafeText)
            ]));

        Assert.Empty(cut.FindAll("script"));
        Assert.Contains(
            unsafeText,
            cut.Find("[data-testid='comment']").TextContent);
    }

    [Fact]
    [Trait("UserStory", "US14")]
    public void CommentForm_WithBlankText_DisablesSubmit()
    {
        using var context = CreateContext(new StubCommentService());

        var cut = RenderForm(context);

        Assert.True(cut.Find("button").HasAttribute("disabled"));
    }

    [Fact]
    [Trait("UserStory", "US14")]
    public void CommentForm_WithValidText_CreatesCommentAndInvokesCallback()
    {
        var service = new StubCommentService();
        using var context = CreateContext(service);
        CommentDto? createdComment = null;

        var cut = context.Render<CommentForm>(parameters => parameters
            .Add(component => component.AnnouncementId, 42)
            .Add(component => component.OnCreated,
                comment => createdComment = comment));

        cut.Find("#comment-content")
            .Change("Er bilen stadig til salg?");
        cut.Find("button").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(service.LastRequest);
            Assert.Equal(42, service.LastRequest.AnnouncementId);
            Assert.Equal(
                "Er bilen stadig til salg?",
                service.LastRequest.Text);
            Assert.NotNull(createdComment);
            Assert.Equal(17, createdComment.Id);
        });
    }

    [Fact]
    [Trait("UserStory", "US14")]
    public void CommentForm_WhenRateLimitIsReached_ShowsHelpfulMessage()
    {
        var service = new StubCommentService
        {
            ExceptionToThrow = new InvalidOperationException(
                "Du har skrevet for mange kommentarer. Vent et øjeblik og prøv igen.")
        };
        using var context = CreateContext(service);
        var cut = RenderForm(context);

        cut.Find("#comment-content").Change("Endnu en kommentar");
        cut.Find("button").Click();

        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find("[role='alert']");
            Assert.Contains(
                "Du har skrevet for mange kommentarer. Prøv igen om lidt.",
                alert.TextContent);
            Assert.DoesNotContain(
                "Kommentaren kunne ikke oprettes",
                alert.TextContent);
        });
    }

    private static BunitContext CreateContext(
        ICommentService service)
    {
        var context = new BunitContext();
        context.Services.AddSingleton(service);
        return context;
    }

    private static IRenderedComponent<CommentForm> RenderForm(
        BunitContext context)
    {
        return context.Render<CommentForm>(parameters =>
            parameters.Add(component => component.AnnouncementId, 42));
    }

    private static CommentDto CreateComment(
        string text = "Er bilen stadig til salg?",
        DateTime? createdAt = null)
    {
        return new CommentDto
        {
            Id = 17,
            AnnouncementId = 42,
            UserId = 9,
            Username = "interesseret-køber",
            Text = text,
            CreatedAt = createdAt
                ?? new DateTime(2026, 8, 24, 10, 30, 0)
        };
    }

    private sealed class StubCommentService : ICommentService
    {
        public CreateCommentDto? LastRequest { get; private set; }
        public Exception? ExceptionToThrow { get; init; }

        public Task<CommentDto> CreateAsync(CreateCommentDto request)
        {
            LastRequest = request;

            if (ExceptionToThrow is not null)
            {
                return Task.FromException<CommentDto>(
                    ExceptionToThrow);
            }

            return Task.FromResult(CreateComment(text: request.Text));
        }

        public Task<IReadOnlyList<CommentDto>> GetForAnnouncementAsync(
            int announcementId) =>
            throw new NotSupportedException();

        public Task DeleteAsync(long commentId) =>
            throw new NotSupportedException();
    }
}
