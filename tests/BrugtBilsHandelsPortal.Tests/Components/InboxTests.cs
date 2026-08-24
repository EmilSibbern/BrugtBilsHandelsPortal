using BrugtBilsHandelsPortal.Components.Pages.Messages;
using BrugtBilsHandelsPortal.DTOs.Messages;
using BrugtBilsHandelsPortal.Services.Interfaces;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BrugtBilsHandelsPortal.Tests.Components;

public class InboxTests
{
    [Fact]
    [Trait("UserStory", "US12")]
    public void Messages_ShowSenderAnnouncementTextAndUnreadStatus()
    {
        using var context = CreateContext(
        [
            CreateMessage(isRead: false)
        ]);

        var cut = context.Render<Inbox>();

        cut.WaitForAssertion(() =>
        {
            var inbox = cut.Find("[data-testid='message-inbox']");
            Assert.Contains("Skoda Octavia", inbox.TextContent);
            Assert.Contains("interesseret-køber", inbox.TextContent);
            Assert.Contains("Er bilen stadig til salg?", inbox.TextContent);
            Assert.Contains("Ny", inbox.TextContent);
        });
    }

    [Fact]
    [Trait("UserStory", "US12")]
    public void ReadMessage_DoesNotShowUnreadBadge()
    {
        using var context = CreateContext(
        [
            CreateMessage(isRead: true)
        ]);

        var cut = context.Render<Inbox>();

        cut.WaitForAssertion(() =>
            Assert.Empty(cut.FindAll(".badge")));
    }

    [Fact]
    [Trait("UserStory", "US12")]
    public void EmptyInbox_ShowsEmptyState()
    {
        using var context = CreateContext([]);

        var cut = context.Render<Inbox>();

        cut.WaitForAssertion(() =>
            Assert.Contains(
                "Du har ingen beskeder endnu.",
                cut.Markup));
    }

    [Fact]
    [Trait("UserStory", "US12")]
    public void ServiceFailure_ShowsSafeErrorState()
    {
        using var context = CreateContext(
            exception: new InvalidOperationException(
                "SELECT Message FROM dbo.Messages"));

        var cut = context.Render<Inbox>();

        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find("[role='alert']");
            Assert.Contains(
                "Beskederne kunne ikke indlæses. Prøv igen.",
                alert.TextContent);
            Assert.DoesNotContain("SELECT", cut.Markup);
        });
    }

    [Fact]
    [Trait("UserStory", "US12")]
    public void MessageText_IsOutputEncoded()
    {
        using var context = CreateContext(
        [
            CreateMessage(
                isRead: false,
                text: "<script>alert('xss')</script>")
        ]);

        var cut = context.Render<Inbox>();

        cut.WaitForAssertion(() =>
        {
            Assert.Empty(cut.FindAll("script"));
            Assert.Contains(
                "<script>alert('xss')</script>",
                cut.Find("[data-testid='message-inbox']")
                    .TextContent);
        });
    }

    private static BunitContext CreateContext(
        IReadOnlyList<MessageDto>? messages = null,
        Exception? exception = null)
    {
        var context = new BunitContext();
        context.Services.AddSingleton<IMessageService>(
            new InboxMessageService(
                messages ?? [],
                exception));
        return context;
    }

    private static MessageDto CreateMessage(
        bool isRead,
        string text = "Er bilen stadig til salg?")
    {
        return new MessageDto
        {
            Id = 12,
            AnnouncementId = 42,
            AnnouncementTitle = "Skoda Octavia",
            SenderUserId = 9,
            SenderUsername = "interesseret-køber",
            ReceiverUserId = 7,
            ReceiverUsername = "sælger",
            Text = text,
            CreatedAt = new DateTime(2026, 8, 24, 10, 30, 0),
            IsRead = isRead,
            ReadAt = isRead
                ? new DateTime(2026, 8, 24, 10, 35, 0)
                : null
        };
    }

    private sealed class InboxMessageService(
        IReadOnlyList<MessageDto> messages,
        Exception? exception) : IMessageService
    {
        public Task<IReadOnlyList<MessageDto>> GetInboxAsync()
        {
            return exception is null
                ? Task.FromResult(messages)
                : Task.FromException<IReadOnlyList<MessageDto>>(
                    exception);
        }

        public Task<MessageDto> SendAsync(SendMessageDto request) =>
            throw new NotSupportedException();

        public Task<MessageDto?> GetByIdAsync(long messageId) =>
            throw new NotSupportedException();

        public Task<ConversationDto?> GetConversationAsync(long messageId) =>
            throw new NotSupportedException();

        public Task<MessageDto> ReplyAsync(long messageId, string text) =>
            throw new NotSupportedException();
    }
}
