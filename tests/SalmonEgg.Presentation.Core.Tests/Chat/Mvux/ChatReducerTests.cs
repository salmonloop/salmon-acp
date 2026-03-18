using SalmonEgg.Presentation.Core.Mvux.Chat;
using Xunit;

namespace SalmonEgg.Presentation.Core.Tests.Chat.Mvux;

public class ChatReducerTests
{
    [Fact]
    public void GivenInitialState_WhenSetSelectedConversation_ThenConversationIdIsUpdated()
    {
        // Arrange
        var initialState = new ChatState();
        var conversationId = "test-conv-123";
        var action = new SelectConversationAction(conversationId);

        // Act
        var newState = ChatReducer.Reduce(initialState, action);

        // Assert
        Assert.Equal(conversationId, newState.SelectedConversationId);
    }

    [Fact]
    public void GivenState_WhenSetPromptInFlight_ThenIsPromptInFlightIsTrue()
    {
        // Arrange
        var initialState = new ChatState(IsPromptInFlight: false);
        var action = new SetPromptInFlightAction(true);

        // Act
        var newState = ChatReducer.Reduce(initialState, action);

        // Assert
        Assert.True(newState.IsPromptInFlight);
    }

    [Fact]
    public void GivenState_WhenSelectProfile_ThenSelectedProfileIdIsUpdated()
    {
        // Arrange
        var initialState = new ChatState(SelectedAcpProfileId: null);
        var profileId = "profile-1";
        var action = new SelectProfileAction(profileId);

        // Act
        var newState = ChatReducer.Reduce(initialState, action);

        // Assert
        Assert.Equal(profileId, newState.SelectedAcpProfileId);
    }

    [Fact]
    public void GivenState_WhenSetDraftText_ThenDraftTextIsUpdated()
    {
        // Arrange
        var initialState = new ChatState(DraftText: string.Empty);
        var action = new SetDraftTextAction("hello");

        // Act
        var newState = ChatReducer.Reduce(initialState, action);

        // Assert
        Assert.Equal("hello", newState.DraftText);
    }
}
