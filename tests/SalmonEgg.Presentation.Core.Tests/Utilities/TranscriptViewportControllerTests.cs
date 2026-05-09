using SalmonEgg.Presentation.Utilities;
using Xunit;

namespace SalmonEgg.Presentation.Core.Tests.Utilities;

public sealed class TranscriptViewportControllerTests
{
    [Fact]
    public void AttachedAppendWhenViewportIsReadyButAwayFromBottom_IssuesNativeScrollRequest()
    {
        var sut = new TranscriptViewportController();
        sut.Load("conv-1", isSessionActive: true, isOverlayVisible: false, hasMessages: true);

        var actions = sut.OnMessagesAppended(1, new TranscriptViewportViewState(
            IsViewReady: true,
            IsViewportReady: true,
            HasMessages: true,
            IsAtBottom: false,
            IsLastItemVisibleAtBottom: false));

        Assert.Contains(actions, action => action.Kind == TranscriptViewportControllerActionKind.ScrollLastMessageIntoView);
        Assert.True(sut.IsAutoFollowAttached);
        Assert.False(sut.IsViewportDetached);
    }

    [Fact]
    public void UserScrollAwayFromBottom_DetachesAndSuppressesAutoFollowForLaterAppends()
    {
        var sut = new TranscriptViewportController();
        sut.Load("conv-1", isSessionActive: true, isOverlayVisible: false, hasMessages: true);

        var detached = sut.OnUserViewportIntent(
            new TranscriptViewportViewState(
                IsViewReady: true,
                IsViewportReady: true,
                HasMessages: true,
                IsAtBottom: false,
                IsLastItemVisibleAtBottom: false),
            new TranscriptProjectionRestoreToken("conv-1", 7, "item-3"));
        var append = sut.OnMessagesAppended(1, new TranscriptViewportViewState(
            IsViewReady: true,
            IsViewportReady: true,
            HasMessages: true,
            IsAtBottom: false,
            IsLastItemVisibleAtBottom: false));

        Assert.Contains(detached, action => action.Kind == TranscriptViewportControllerActionKind.AutoFollowDetached);
        Assert.DoesNotContain(append, action => action.Kind == TranscriptViewportControllerActionKind.ScrollLastMessageIntoView);
        Assert.True(sut.IsViewportDetached);
        Assert.False(sut.IsAutoFollowAttached);
    }

    [Fact]
    public void ExplicitAwayIntentAtBottom_DetachesBeforeNativeViewportObservation()
    {
        var sut = new TranscriptViewportController();
        sut.Load("conv-1", isSessionActive: true, isOverlayVisible: false, hasMessages: true);

        var actions = sut.OnUserViewportDetachIntent(
            new TranscriptViewportViewState(
                IsViewReady: true,
                IsViewportReady: true,
                HasMessages: true,
                IsAtBottom: true,
                IsLastItemVisibleAtBottom: true),
            new TranscriptProjectionRestoreToken("conv-1", 7, "item-9"));

        Assert.Contains(actions, action => action.Kind == TranscriptViewportControllerActionKind.AutoFollowDetached);
        Assert.True(sut.IsViewportDetached);
        Assert.False(sut.IsAutoFollowAttached);
    }


    [Fact]
    public void DetachedUserReturnsToBottom_ReattachesAndNextAppendAutoFollows()
    {
        var sut = new TranscriptViewportController();
        sut.Load("conv-1", isSessionActive: true, isOverlayVisible: false, hasMessages: true);
        _ = sut.OnUserViewportIntent(
            new TranscriptViewportViewState(
                IsViewReady: true,
                IsViewportReady: true,
                HasMessages: true,
                IsAtBottom: false,
                IsLastItemVisibleAtBottom: false),
            new TranscriptProjectionRestoreToken("conv-1", 7, "item-3"));

        var attached = sut.OnUserViewportIntent(new TranscriptViewportViewState(
            IsViewReady: true,
            IsViewportReady: true,
            HasMessages: true,
            IsAtBottom: true,
            IsLastItemVisibleAtBottom: true));
        var append = sut.OnMessagesAppended(1, new TranscriptViewportViewState(
            IsViewReady: true,
            IsViewportReady: true,
            HasMessages: true,
            IsAtBottom: false,
            IsLastItemVisibleAtBottom: false));

        Assert.Contains(attached, action => action.Kind == TranscriptViewportControllerActionKind.AutoFollowAttached);
        Assert.Contains(append, action => action.Kind == TranscriptViewportControllerActionKind.ScrollLastMessageIntoView);
        Assert.False(sut.IsViewportDetached);
        Assert.True(sut.IsAutoFollowAttached);
    }

    [Fact]
    public void ConversationChange_InvalidatesQueuedScrollFromPreviousConversation()
    {
        var sut = new TranscriptViewportController();
        sut.Load("conv-1", isSessionActive: true, isOverlayVisible: false, hasMessages: true);
        _ = sut.OnMessagesAppended(1, new TranscriptViewportViewState(
            IsViewReady: true,
            IsViewportReady: true,
            HasMessages: true,
            IsAtBottom: false,
            IsLastItemVisibleAtBottom: false));
        Assert.True(sut.TryCaptureActiveScrollRequest(out var staleRequest));

        sut.OnConversationChanged("conv-2", isSessionActive: true, isOverlayVisible: false, hasMessages: true);

        var actions = sut.OnScheduledScrollObservation(
            staleRequest,
            new TranscriptViewportViewState(
                IsViewReady: true,
                IsViewportReady: true,
                HasMessages: true,
                IsAtBottom: true,
                IsLastItemVisibleAtBottom: true));

        Assert.Empty(actions);
        Assert.False(sut.MatchesActiveScrollRequest(staleRequest));
    }
}
