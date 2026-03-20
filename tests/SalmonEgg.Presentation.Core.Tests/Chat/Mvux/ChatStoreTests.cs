using System.Threading;
using System.Threading.Tasks;
using SalmonEgg.Presentation.Core.Mvux.Chat;
using Uno.Extensions.Reactive;
using Xunit;

namespace SalmonEgg.Presentation.Core.Tests.Chat.Mvux;

public class ChatStoreTests
{
    [Fact]
    public async Task GivenStore_WhenDispatchAction_ThenStateIsUpdatedViaReducer()
    {
        // Arrange
        var initialState = new ChatState(SelectedConversationId: "initial");
        await using var state = State.Value(this, () => initialState);
        var store = new ChatStore(state);
        var newConversationId = "updated-id";
        var action = new SelectConversationAction(newConversationId);

        // Act
        await store.Dispatch(action);

        // Assert
        var currentState = await state;
        Assert.NotNull(currentState);
        Assert.Equal(newConversationId, currentState.SelectedConversationId);
    }

    [Fact]
    public async Task GivenStore_WhenMultipleDispatches_ThenStateTransitionsSequentially()
    {
        // Arrange
        await using var state = State.Value(this, () => ChatState.Empty);
        var store = new ChatStore(state);

        // Act
        await store.Dispatch(new SetPromptInFlightAction(true));
        await store.Dispatch(new SelectConversationAction("conv-1"));

        // Assert
        var currentState = await state;
        Assert.NotNull(currentState);
        Assert.False(currentState.IsPromptInFlight);
        Assert.Equal("conv-1", currentState.SelectedConversationId);
    }
}
