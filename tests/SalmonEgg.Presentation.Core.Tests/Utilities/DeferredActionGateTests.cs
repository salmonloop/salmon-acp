using System;
using SalmonEgg.Presentation.Utilities;
using Xunit;

namespace SalmonEgg.Presentation.Core.Tests.Utilities;

public sealed class DeferredActionGateTests
{
    [Fact]
    public void TryConsume_ReturnsFalse_WhenNoPending()
    {
        var gate = new DeferredActionGate<string>(StringComparer.Ordinal);
        var consumed = gate.TryConsume("session-1");

        Assert.False(consumed);
    }

    [Fact]
    public void Request_Then_TryConsume_MatchingKey_InvokesAction()
    {
        var gate = new DeferredActionGate<string>(StringComparer.Ordinal);
        var invoked = false;

        gate.Request("session-1", () => invoked = true);

        var consumed = gate.TryConsume("session-1");

        Assert.True(consumed);
        Assert.True(invoked);
    }

    [Fact]
    public void Request_LastWins_WhenReplaced()
    {
        var gate = new DeferredActionGate<string>(StringComparer.Ordinal);
        var firstInvoked = false;
        var secondInvoked = false;

        gate.Request("session-1", () => firstInvoked = true);
        gate.Request("session-2", () => secondInvoked = true);

        var firstConsumed = gate.TryConsume("session-1");
        var secondConsumed = gate.TryConsume("session-2");

        Assert.False(firstConsumed);
        Assert.True(secondConsumed);
        Assert.False(firstInvoked);
        Assert.True(secondInvoked);
    }

    [Fact]
    public void Clear_RemovesPending()
    {
        var gate = new DeferredActionGate<string>(StringComparer.Ordinal);
        var invoked = false;

        gate.Request("session-1", () => invoked = true);
        gate.Clear();

        var consumed = gate.TryConsume("session-1");

        Assert.False(consumed);
        Assert.False(invoked);
    }
}
