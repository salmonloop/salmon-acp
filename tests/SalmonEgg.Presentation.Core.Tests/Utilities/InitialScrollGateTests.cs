using SalmonEgg.Presentation.Utilities;
using Xunit;

namespace SalmonEgg.Presentation.Core.Tests.Utilities;

public sealed class InitialScrollGateTests
{
    [Fact]
    public void TrySchedule_ReturnsFalse_WhenNoItems()
    {
        var gate = new InitialScrollGate();

        var scheduled = gate.TrySchedule(0);
        var scheduledAfterItems = gate.TrySchedule(1);

        Assert.False(scheduled);
        Assert.True(scheduledAfterItems);
    }

    [Fact]
    public void TrySchedule_ReturnsFalse_WhenAlreadyInFlight()
    {
        var gate = new InitialScrollGate();

        var first = gate.TrySchedule(1);
        var second = gate.TrySchedule(1);

        Assert.True(first);
        Assert.False(second);
    }

    [Fact]
    public void TryComplete_ReturnsFalse_WhenItemsCleared_AndKeepsPending()
    {
        var gate = new InitialScrollGate();

        var scheduled = gate.TrySchedule(1);
        var completed = gate.TryComplete(0);
        var scheduledAgain = gate.TrySchedule(1);

        Assert.True(scheduled);
        Assert.False(completed);
        Assert.True(scheduledAgain);
    }

    [Fact]
    public void TryComplete_ClearsPending_WhenItemsAvailable()
    {
        var gate = new InitialScrollGate();

        var scheduled = gate.TrySchedule(1);
        var completed = gate.TryComplete(1);
        var scheduledAfterComplete = gate.TrySchedule(1);
        gate.MarkPending();
        var scheduledAfterReset = gate.TrySchedule(1);

        Assert.True(scheduled);
        Assert.True(completed);
        Assert.False(scheduledAfterComplete);
        Assert.True(scheduledAfterReset);
    }
}
