using BrugtBilsHandelsPortal.DTOs.Messages;
using BrugtBilsHandelsPortal.Services.Interfaces;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ConversationPage =
    BrugtBilsHandelsPortal.Components.Pages.Messages.Conversation;

namespace BrugtBilsHandelsPortal.Tests.Components;

public class ConversationTests
{
    [Fact]
    [Trait("UserStory", "US13")]
    public void Conversation_ShowsBothDirectionsAndAnnouncementLink()
    {
        using var context = CreateContext(new StubMessageService());

        var cut = RenderConversation(context);

        cut.WaitForAssertion(() =>
        {
            var messages =
                cut.Find("[data-testid='conversation-messages']");
            Assert.Contains("Spørgsmål fra køber", messages.TextContent);
            Assert.Contains("Svar fra sælger", messages.TextContent);
            Assert.Contains("interesseret-køber", cut.Markup);
            Assert.Equal(
                "/announcements/42",
                cut.Find("a[href='/announcements/42']")
                    .GetAttribute("href"));
        });
    }

    [Fact]
    [Trait("UserStory", "US13")]
    public void ValidReply_CallsServiceAndShowsSuccessFeedback()
    {
        var service = new StubMessageService();
        using var context = CreateContext(service);
        var cut = RenderConversation(context);

        cut.WaitForElement("#reply-text")
            .Change("  Tak for svaret  ");
        cut.Find("button[type='submit']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(12, service.LastReplyMessageId);
            Assert.Equal("  Tak for svaret  ", service.LastReplyText);
            Assert.Equal(1, service.ReplyCallCount);
            Assert.Contains("Dit svar er sendt.", cut.Markup);
        });
    }

    [Fact]
    [Trait("UserStory", "US13")]
    public void MissingOrForbiddenConversation_ShowsSafeState()
    {
        using var context = CreateContext(
            new StubMessageService
            {
                Conversation = null
            });

        var cut = RenderConversation(context);

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(
                "Samtalen findes ikke, eller du har ikke adgang til den.",
                cut.Markup);
            Assert.Empty(cut.FindAll("#reply-text"));
        });
    }

    [Fact]
    [Trait("UserStory", "US13")]
    public void MessageText_IsOutputEncoded()
    {
        var service = new StubMessageService
        {
            Conversation = CreateConversation(
                "<script>alert('xss')</script>")
        };
        using var context = CreateContext(service);

        var cut = RenderConversation(context);

        cut.WaitForAssertion(() =>
        {
            Assert.Empty(cut.FindAll("script"));
            Assert.Contains(
                "<script>alert('xss')</script>",
                cut.Find("[data-testid='conversation-messages']")
                    .TextContent);
        });
    }

    private static BunitContext CreateContext(
        IMessageService service)
    {
        var context = new BunitContext();
        context.Services.AddSingleton(service);
        return context;
    }

    private static IRenderedComponent<ConversationPage>
        RenderConversation(BunitContext context)
    {
        return context.Render<ConversationPage>(parameters =>
            parameters.Add(component => component.MessageId, 12));
    }

    private static ConversationDto CreateConversation(
        string firstText = "Spørgsmål fra køber")
    {
        return new ConversationDto
        {
            AnnouncementId = 42,
            AnnouncementTitle = "Skoda Octavia",
            OtherUserId = 9,
            OtherUsername = "interesseret-køber",
            Messages =
            [
                CreateMessage(
                    12,
                    senderUserId: 9,
                    receiverUserId: 7,
                    firstText),
                CreateMessage(
                    13,
                    senderUserId: 7,
                    receiverUserId: 9,
                    "Svar fra sælger")
            ]
        };
    }

    private static MessageDto CreateMessage(
        long id,
        int senderUserId,
        int receiverUserId,
        string text)
    {
        return new MessageDto
        {
            Id = id,
            AnnouncementId = 42,
            AnnouncementTitle = "Skoda Octavia",
            SenderUserId = senderUserId,
            SenderUsername = senderUserId == 9
                ? "interesseret-køber"
                : "sælger",
            ReceiverUserId = receiverUserId,
            ReceiverUsername = receiverUserId == 9
                ? "interesseret-køber"
                : "sælger",
            Text = text,
            CreatedAt = new DateTime(2026, 8, 24, 10, 30, 0),
            IsRead = true,
            ReadAt = new DateTime(2026, 8, 24, 10, 31, 0)
        };
    }

    private sealed class StubMessageService : IMessageService
    {
        public ConversationDto? Conversation { get; init; }
            = CreateConversation();

        public int ReplyCallCount { get; private set; }
        public long? LastReplyMessageId { get; private set; }
        public string? LastReplyText { get; private set; }

        public Task<ConversationDto?> GetConversationAsync(
            long messageId) =>
            Task.FromResult(Conversation);

        public Task<MessageDto> ReplyAsync(
            long messageId,
            string text)
        {
            ReplyCallCount++;
            LastReplyMessageId = messageId;
            LastReplyText = text;

            return Task.FromResult(
                CreateMessage(
                    14,
                    senderUserId: 7,
                    receiverUserId: 9,
                    text));
        }

        public Task<MessageDto> SendAsync(SendMessageDto request) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<MessageDto>> GetInboxAsync() =>
            throw new NotSupportedException();

        public Task<MessageDto?> GetByIdAsync(long messageId) =>
            throw new NotSupportedException();
    }
}
