using System;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using SalmonEgg.Infrastructure.Client;
using SalmonEgg.Infrastructure.Network;
using Xunit;

namespace SalmonEgg.Infrastructure.Tests.Client;

public sealed class NetworkTransportAdapterTests
{
    [Fact]
    public void MessageReceived_Should_Raise_For_NonEmpty_Messages()
    {
        var messages = new Subject<string>();
        var states = new Subject<TransportState>();
        var inner = new Mock<ITransport>();
        inner.SetupGet(x => x.Messages).Returns(messages);
        inner.SetupGet(x => x.StateChanges).Returns(states);

        var adapter = new NetworkTransportAdapter(inner.Object, "wss://example.com");
        var received = string.Empty;
        adapter.MessageReceived += (_, args) => received = args.Message;

        messages.OnNext("hello");

        Assert.Equal("hello", received);
    }

    [Fact]
    public void MessageReceived_Should_Ignore_Empty_Messages()
    {
        var messages = new Subject<string>();
        var states = new Subject<TransportState>();
        var inner = new Mock<ITransport>();
        inner.SetupGet(x => x.Messages).Returns(messages);
        inner.SetupGet(x => x.StateChanges).Returns(states);

        var adapter = new NetworkTransportAdapter(inner.Object, "wss://example.com");
        var raised = false;
        adapter.MessageReceived += (_, _) => raised = true;

        messages.OnNext(string.Empty);

        Assert.False(raised);
    }

    [Fact]
    public void StateChanges_Should_Update_IsConnected()
    {
        var messages = new Subject<string>();
        var states = new Subject<TransportState>();
        var inner = new Mock<ITransport>();
        inner.SetupGet(x => x.Messages).Returns(messages);
        inner.SetupGet(x => x.StateChanges).Returns(states);

        var adapter = new NetworkTransportAdapter(inner.Object, "https://example.com/events");

        states.OnNext(TransportState.Connected);
        Assert.True(adapter.IsConnected);

        states.OnNext(TransportState.Disconnected);
        Assert.False(adapter.IsConnected);
    }

    [Fact]
    public async Task SendMessageAsync_Should_Return_False_When_Message_Empty()
    {
        var messages = new Subject<string>();
        var states = new Subject<TransportState>();
        var inner = new Mock<ITransport>();
        inner.SetupGet(x => x.Messages).Returns(messages);
        inner.SetupGet(x => x.StateChanges).Returns(states);

        var adapter = new NetworkTransportAdapter(inner.Object, "https://example.com/events");

        var result = await adapter.SendMessageAsync(" ", CancellationToken.None);

        Assert.False(result);
        inner.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ConnectAsync_Should_Return_False_And_Raise_Error_On_Exception()
    {
        var messages = new Subject<string>();
        var states = new Subject<TransportState>();
        var inner = new Mock<ITransport>();
        inner.SetupGet(x => x.Messages).Returns(messages);
        inner.SetupGet(x => x.StateChanges).Returns(states);
        inner.Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("fail"));

        var adapter = new NetworkTransportAdapter(inner.Object, "wss://example.com/socket");
        var errorRaised = false;
        adapter.ErrorOccurred += (_, _) => errorRaised = true;

        var result = await adapter.ConnectAsync(CancellationToken.None);

        Assert.False(result);
        Assert.True(errorRaised);
    }
}
