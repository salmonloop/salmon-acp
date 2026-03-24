using System;
using System.Reflection;
using System.Text.Json;
using SalmonEgg.Infrastructure.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Moq;
using SalmonEgg.Domain.Interfaces.Transport;
using SalmonEgg.Domain.Models.Content;
using SalmonEgg.Domain.Models.JsonRpc;
using SalmonEgg.Domain.Models.Session;
using SalmonEgg.Domain.Models.Protocol;
using SalmonEgg.Domain.Models.Mcp;
using SalmonEgg.Domain.Services;
using SalmonEgg.Infrastructure.Client;
using Xunit;
using SalmonEgg.Domain.Interfaces;

namespace SalmonEgg.Infrastructure.Tests.Client
{
    public class AcpClientTests
    {
        private readonly Mock<ITransport> _transportMock = new();
        private readonly Mock<IMessageParser> _parserMock = new();
        private readonly Mock<IErrorLogger> _errorLoggerMock = new();

        public AcpClientTests()
        {
            _transportMock.SetupGet(t => t.IsConnected).Returns(true);
            _parserMock.Setup(p => p.Options).Returns(new JsonSerializerOptions());
        }

        private async Task<AcpClient> CreateInitializedClientAsync(AcpClient.AcpRequestTimeouts? timeouts = null)
        {
            var parser = new MessageParser(); // Use real parser for serialization
            
            var initTimeouts = timeouts ?? new AcpClient.AcpRequestTimeouts(
                DefaultTimeout: TimeSpan.FromSeconds(5),
                SessionNewTimeout: TimeSpan.FromSeconds(5),
                SessionPromptTimeout: TimeSpan.FromSeconds(5));
            if (initTimeouts.DefaultTimeout < TimeSpan.FromSeconds(5))
            {
                initTimeouts = initTimeouts with { DefaultTimeout = TimeSpan.FromSeconds(5) };
            }

            var client = new AcpClient(_transportMock.Object, parser, null, _errorLoggerMock.Object, initTimeouts);

            // Mock InitializeAsync response
            var initResponse = new InitializeResponse(
                1, // protocolVersion
                new AgentInfo("TestAgent", "1.0.0"),
                new AgentCapabilities()
            );

            _transportMock.Setup(t => t.SendMessageAsync(It.IsRegex("initialize"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var initTrigger = Task.Run(async () => {
                await Task.Delay(10);
                var response = new JsonRpcResponse(1, JsonSerializer.SerializeToElement(initResponse, parser.Options));
                _transportMock.Raise(t => t.MessageReceived += null, new MessageReceivedEventArgs(parser.SerializeMessage(response)));
            });

            await client.InitializeAsync(new InitializeParams(new ClientInfo("Test", "1.0.0"), new ClientCapabilities()));
            await initTrigger;

            if (timeouts != null)
            {
                typeof(AcpClient).GetField("_timeouts", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.SetValue(client, timeouts);
            }

            return client;
        }

        [Fact]
        public async Task CreateSessionAsync_SlowButValidResponse_UsesSessionNewTimeoutBudget()
        {
            var timeouts = new AcpClient.AcpRequestTimeouts(
                DefaultTimeout: TimeSpan.FromMilliseconds(50),
                SessionNewTimeout: TimeSpan.FromMilliseconds(500),
                SessionPromptTimeout: TimeSpan.FromMilliseconds(500)
            );

            var parser = new MessageParser();
            var client = await CreateInitializedClientAsync(timeouts);
            
            _transportMock.Setup(t => t.SendMessageAsync(It.IsRegex("session/new"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Delay response for 200ms (exceeds default 50ms, but within session/new 500ms)
            var responseTrigger = Task.Run(async () => {
                await Task.Delay(200);
                var response = new JsonRpcResponse(2, JsonSerializer.SerializeToElement(new SessionNewResponse("session-123"), parser.Options));
                _transportMock.Raise(t => t.MessageReceived += null, new MessageReceivedEventArgs(parser.SerializeMessage(response)));
            });

            var result = await client.CreateSessionAsync(new SessionNewParams("cwd", null));
            await responseTrigger;
            Assert.Equal("session-123", result.SessionId);
        }

        [Fact]
        public async Task NonPromptRequest_StillUsesDefaultTimeoutBudget()
        {
            var timeouts = new AcpClient.AcpRequestTimeouts(
                DefaultTimeout: TimeSpan.FromMilliseconds(50),
                SessionNewTimeout: TimeSpan.FromMilliseconds(500),
                SessionPromptTimeout: TimeSpan.FromMilliseconds(500)
            );

            var parser = new MessageParser();
            var client = await CreateInitializedClientAsync(timeouts);

            _transportMock.Setup(t => t.SendMessageAsync(It.IsRegex("session/load"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Delay response for 200ms (exceeds default 50ms)
            var responseTrigger = Task.Run(async () => {
                await Task.Delay(200);
                var response = new JsonRpcResponse(2, JsonSerializer.SerializeToElement(new { }, parser.Options));
                _transportMock.Raise(t => t.MessageReceived += null, new MessageReceivedEventArgs(parser.SerializeMessage(response)));
            });

            await Assert.ThrowsAsync<TimeoutException>(() => client.LoadSessionAsync(new SessionLoadParams("session-123", "cwd", null)));
            await responseTrigger;
        }

        [Fact]
        public async Task CreateSessionAsync_TimeoutMessage_ContainsMethodAndLastRx()
        {
            var timeouts = new AcpClient.AcpRequestTimeouts(
                DefaultTimeout: TimeSpan.FromMilliseconds(20),
                SessionNewTimeout: TimeSpan.FromMilliseconds(20),
                SessionPromptTimeout: TimeSpan.FromMilliseconds(20)
            );

            var client = await CreateInitializedClientAsync(timeouts);
            
            var ex = await Assert.ThrowsAsync<TimeoutException>(() => client.CreateSessionAsync(new SessionNewParams("cwd", null)));
            
            Assert.Contains("method=session/new", ex.Message);
            Assert.Contains("timeout=", ex.Message);
            Assert.Contains("lastRx=", ex.Message);
        }

        [Theory]
        [InlineData(StopReason.MaxTurnRequests)]
        [InlineData(StopReason.Refusal)]
        [InlineData(StopReason.Cancelled)]
        public async Task SendPromptAsync_ParsesOfficialStopReasonValues(StopReason expected)
        {
            var parser = new MessageParser();
            var client = await CreateInitializedClientAsync();

            _transportMock.Setup(t => t.SendMessageAsync(It.IsRegex("session/new"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _transportMock.Setup(t => t.SendMessageAsync(It.IsRegex("session/prompt"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var createResponseTrigger = Task.Run(async () => {
                await Task.Delay(10);
                var response = new JsonRpcResponse(2, JsonSerializer.SerializeToElement(new SessionNewResponse("session-123"), parser.Options));
                _transportMock.Raise(t => t.MessageReceived += null, new MessageReceivedEventArgs(parser.SerializeMessage(response)));
            });

            var createResult = await client.CreateSessionAsync(new SessionNewParams("cwd", null));
            await createResponseTrigger;

            var promptResponseTrigger = Task.Run(async () => {
                await Task.Delay(10);
                var response = new JsonRpcResponse(3, JsonSerializer.SerializeToElement(new SessionPromptResponse(expected), parser.Options));
                _transportMock.Raise(t => t.MessageReceived += null, new MessageReceivedEventArgs(parser.SerializeMessage(response)));
            });

            var promptResult = await client.SendPromptAsync(new SessionPromptParams(createResult.SessionId, new List<ContentBlock> { new TextContentBlock("hi") }));
            await promptResponseTrigger;

            Assert.Equal(expected, promptResult.StopReason);
        }

        [Fact]
        public async Task SendPromptAsync_TimeoutAfterOtherSessionTraffic_PropagatesTimeout()
        {
            var parser = new MessageParser();
            var timeouts = new AcpClient.AcpRequestTimeouts(
                DefaultTimeout: TimeSpan.FromMilliseconds(50),
                SessionNewTimeout: TimeSpan.FromMilliseconds(50),
                SessionPromptTimeout: TimeSpan.FromMilliseconds(50));

            var client = await CreateInitializedClientAsync(timeouts);

            _transportMock.Setup(t => t.SendMessageAsync(It.IsRegex("session/new"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _transportMock.Setup(t => t.SendMessageAsync(It.IsRegex("session/prompt"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var createResponseTrigger = Task.Run(async () =>
            {
                await Task.Delay(10);
                var response = new JsonRpcResponse(2, JsonSerializer.SerializeToElement(new SessionNewResponse("session-123"), parser.Options));
                _transportMock.Raise(t => t.MessageReceived += null, new MessageReceivedEventArgs(parser.SerializeMessage(response)));
            });

            var createResult = await client.CreateSessionAsync(new SessionNewParams("cwd", null));
            await createResponseTrigger;

            var promptTask = client.SendPromptAsync(new SessionPromptParams(createResult.SessionId, new List<ContentBlock> { new TextContentBlock("hi") }));

            await Task.Delay(20);
            var unrelatedUpdate = new JsonRpcNotification(
                "session/update",
                JsonSerializer.SerializeToElement(new SessionUpdateParams("other-session", new UsageUpdate { Used = 1 }), parser.Options));
            _transportMock.Raise(t => t.MessageReceived += null, new MessageReceivedEventArgs(parser.SerializeMessage(unrelatedUpdate)));

            await Assert.ThrowsAsync<TimeoutException>(() => promptTask);
        }

        [Fact]
        public async Task SendPromptAsync_TimeoutAfterSameSessionTraffic_StillPropagatesTimeout()
        {
            var parser = new MessageParser();
            var timeouts = new AcpClient.AcpRequestTimeouts(
                DefaultTimeout: TimeSpan.FromMilliseconds(50),
                SessionNewTimeout: TimeSpan.FromMilliseconds(50),
                SessionPromptTimeout: TimeSpan.FromMilliseconds(50));

            var client = await CreateInitializedClientAsync(timeouts);

            _transportMock.Setup(t => t.SendMessageAsync(It.IsRegex("session/new"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _transportMock.Setup(t => t.SendMessageAsync(It.IsRegex("session/prompt"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var createResponseTrigger = Task.Run(async () =>
            {
                await Task.Delay(10);
                var response = new JsonRpcResponse(2, JsonSerializer.SerializeToElement(new SessionNewResponse("session-123"), parser.Options));
                _transportMock.Raise(t => t.MessageReceived += null, new MessageReceivedEventArgs(parser.SerializeMessage(response)));
            });

            var createResult = await client.CreateSessionAsync(new SessionNewParams("cwd", null));
            await createResponseTrigger;

            var promptTask = client.SendPromptAsync(new SessionPromptParams(createResult.SessionId, new List<ContentBlock> { new TextContentBlock("hi") }));

            await Task.Delay(20);
            var sameSessionUpdate = new JsonRpcNotification(
                "session/update",
                JsonSerializer.SerializeToElement(new SessionUpdateParams(
                    createResult.SessionId,
                    new AgentThoughtUpdate
                    {
                        Content = new TextContentBlock("thinking")
                    }), parser.Options));
            _transportMock.Raise(t => t.MessageReceived += null, new MessageReceivedEventArgs(parser.SerializeMessage(sameSessionUpdate)));

            await Assert.ThrowsAsync<TimeoutException>(() => promptTask);
        }
    }
}
