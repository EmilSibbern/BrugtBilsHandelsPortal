using BrugtBilsHandelsPortal.Components.Shared;
using BrugtBilsHandelsPortal.DTOs.Messages;
using BrugtBilsHandelsPortal.Services.Interfaces;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BrugtBilsHandelsPortal.Tests.Components;

public class ContactSellerFormTests
{
    [Fact]
    [Trait("UserStory", "US12")]
    public void EmptyMessage_DisablesSubmitButton()
    {
        using var context = CreateContext(
            new StubMessageService());

        var cut = RenderForm(context);

        Assert.True(
            cut.Find("[data-testid='send-message']")
                .HasAttribute("disabled"));
    }

    [Fact]
    [Trait("UserStory", "US12")]
    public void Textarea_ShowsCurrentCharacterCountAndMaximum()
    {
        using var context = CreateContext(
            new StubMessageService());

        var cut = RenderForm(context);
        cut.Find("#seller-message").Input("Hej!");

        Assert.Contains("4 / 2000 tegn", cut.Markup);
        Assert.Equal(
            "2000",
            cut.Find("#seller-message")
                .GetAttribute("maxlength"));
    }

    [Fact]
    [Trait("UserStory", "US12")]
    public void ValidMessage_CallsServiceAndShowsSuccessFeedback()
    {
        using var context = new BunitContext();
        var service = new StubMessageService();
        context.Services.AddSingleton<IMessageService>(service);

        MessageDto? sentMessage = null;
        var cut = context.Render<ContactSellerForm>(parameters => parameters
            .Add(component => component.AnnouncementId, 42)
            .Add(component => component.OnSent,
                message => sentMessage = message));

        cut.Find("#seller-message")
            .Input("  Er bilen stadig til salg?  ");
        cut.Find("[data-testid='send-message']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(service.LastRequest);
            Assert.Equal(42, service.LastRequest.AnnouncementId);
            Assert.Equal(
                "Er bilen stadig til salg?",
                service.LastRequest.Text);
            Assert.Equal(1, service.SendCallCount);
            Assert.NotNull(sentMessage);
            Assert.Contains(
                "Beskeden er sendt til sælgeren.",
                cut.Markup);
        });
    }

    [Fact]
    [Trait("UserStory", "US12")]
    public void ServiceFailure_ShowsSafeErrorFeedback()
    {
        using var context = CreateContext(
            new StubMessageService
            {
                ExceptionToThrow = new InvalidOperationException(
                    "SELECT * FROM Messages")
            });

        var cut = RenderForm(context);
        cut.Find("#seller-message").Input("Hej sælger");
        cut.Find("[data-testid='send-message']").Click();

        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find("[role='alert']");
            Assert.Contains(
                "Beskeden kunne ikke sendes. Prøv igen.",
                alert.TextContent);
            Assert.DoesNotContain("SELECT", cut.Markup);
        });
    }

    [Fact]
    [Trait("UserStory", "US12")]
    public void Cancel_InvokesCancelCallback()
    {
        using var context = new BunitContext();
        context.Services.AddSingleton<IMessageService>(
            new StubMessageService());

        var wasCancelled = false;
        var cut = context.Render<ContactSellerForm>(parameters => parameters
            .Add(component => component.AnnouncementId, 42)
            .Add(component => component.OnCancel,
                () => wasCancelled = true));

        cut.Find("button.btn-outline-secondary").Click();

        Assert.True(wasCancelled);
    }

    private static BunitContext CreateContext(
        IMessageService service)
    {
        var context = new BunitContext();
        context.Services.AddSingleton(service);
        return context;
    }

    private static IRenderedComponent<ContactSellerForm> RenderForm(
        BunitContext context)
    {
        return context.Render<ContactSellerForm>(parameters =>
            parameters.Add(
                component => component.AnnouncementId,
                42));
    }

    private sealed class StubMessageService : IMessageService
    {
        public SendMessageDto? LastRequest { get; private set; }
        public int SendCallCount { get; private set; }
        public Exception? ExceptionToThrow { get; init; }

        public Task<MessageDto> SendAsync(SendMessageDto request)
        {
            SendCallCount++;
            LastRequest = request;

            if (ExceptionToThrow is not null)
            {
                return Task.FromException<MessageDto>(
                    ExceptionToThrow);
            }

            return Task.FromResult(
                new MessageDto
                {
                    Id = 1,
                    AnnouncementId = request.AnnouncementId,
                    SenderUserId = 9,
                    ReceiverUserId = 7,
                    Text = request.Text,
                    CreatedAt = new DateTime(2026, 8, 24)
                });
        }

        public Task<IReadOnlyList<MessageDto>> GetInboxAsync() =>
            throw new NotSupportedException();

        public Task<MessageDto?> GetByIdAsync(long messageId) =>
            throw new NotSupportedException();
    }
}
