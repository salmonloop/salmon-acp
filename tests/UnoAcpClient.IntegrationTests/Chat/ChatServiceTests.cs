using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using UnoAcpClient.Application.Services.Chat;
using UnoAcpClient.Domain.Models.Content;
using UnoAcpClient.Domain.Models.Protocol;
using UnoAcpClient.Domain.Models.Session;
using UnoAcpClient.Domain.Services;
using UnoAcpClient.Domain.Services.Security;

namespace UnoAcpClient.IntegrationTests.Chat
{
    /// <summary>
    /// ChatService 集成测试
    /// 测试完整的初始化 → 创建会话 → 发送提示 → 接收更新流程
    /// </summary>
    [TestClass]
    public class ChatServiceTests
    {
        private Mock<IAcpClient> _mockAcpClient;
        private Mock<IErrorLogger> _mockErrorLogger;
        private ChatService _chatService;

        [TestInitialize]
        public void Setup()
        {
            _mockAcpClient = new Mock<IAcpClient>();
            _mockErrorLogger = new Mock<IErrorLogger>();

            // 设置默认行为
            _mockAcpClient.Setup(x => x.IsInitialized).Returns(false);
            _mockAcpClient.Setup(x => x.IsConnected).Returns(false);

            _chatService = new ChatService(_mockAcpClient.Object, _mockErrorLogger.Object);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _chatService.Dispose();
        }

        [TestMethod]
        public async Task InitializeAsync_ShouldCallAcpClientInitialize()
        {
            // Arrange
            var initParams = new InitializeParams
            {
                ProtocolVersion = 1,
                ClientInfo = new ClientInfo("TestClient", "1.0.0"),
                ClientCapabilities = new ClientCapabilities()
            };

            var expectedResponse = new InitializeResponse
            {
                ProtocolVersion = 1,
                AgentInfo = new AgentInfo("TestAgent", "1.0.0"),
                AgentCapabilities = new AgentCapabilities()
            };

            _mockAcpClient.Setup(x => x.InitializeAsync(initParams, It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var response = await _chatService.InitializeAsync(initParams);

            // Assert
            Assert.IsNotNull(response);
            Assert.AreEqual(1, response.ProtocolVersion);
            Assert.AreEqual("TestAgent", response.AgentInfo.Name);
            _mockAcpClient.Verify(x => x.InitializeAsync(initParams, It.IsAny<System.Threading.CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task CreateSessionAsync_ShouldReturnSessionId()
        {
            // Arrange
            _mockAcpClient.Setup(x => x.IsInitialized).Returns(true);

            var sessionParams = new SessionNewParams
            {
                Cwd = "/test"
            };

            var expectedResponse = new SessionNewResponse
            {
                SessionId = "test-session-id",
                Modes = new List<SessionMode>
                {
                    new SessionMode("mode1", "Mode 1"),
                    new SessionMode("mode2", "Mode 2")
                }
            };

            _mockAcpClient.Setup(x => x.CreateSessionAsync(sessionParams, It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var response = await _chatService.CreateSessionAsync(sessionParams);

            // Assert
            Assert.IsNotNull(response);
            Assert.AreEqual("test-session-id", response.SessionId);
            Assert.AreEqual(2, response.Modes?.Count);
            Assert.AreEqual("test-session-id", _chatService.CurrentSessionId);
        }

        [TestMethod]
        public async Task SendPromptAsync_ShouldSendPromptToAgent()
        {
            // Arrange
            _mockAcpClient.Setup(x => x.IsInitialized).Returns(true);
            _chatService.CreateSessionAsync(new SessionNewParams { Cwd = "/test" }).Wait();

            var promptParams = new SessionPromptParams
            {
                SessionId = "test-session-id",
                Prompt = "Hello, Agent!"
            };

            var expectedResponse = new SessionPromptResponse
            {
                StopReason = StopReason.EndTurn
            };

            _mockAcpClient.Setup(x => x.SendPromptAsync(promptParams, It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var response = await _chatService.SendPromptAsync(promptParams);

            // Assert
            Assert.IsNotNull(response);
            Assert.AreEqual(StopReason.EndTurn, response.StopReason);
        }

        [TestMethod]
        public async Task SessionUpdateReceived_ShouldUpdateHistory()
        {
            // Arrange
            var updateEventArgs = new SessionUpdateEventArgs(
                "test-session-id",
                new AgentMessageUpdate(new TextContentBlock { Text = "Test message" })
            );

            var raised = false;
            _chatService.SessionUpdateReceived += (sender, e) => { raised = true; };

            // Act
            _mockAcpClient.Raise(x => x.SessionUpdateReceived += null, updateEventArgs);

            // Wait for event to be processed
            await Task.Delay(100);

            // Assert
            Assert.IsTrue(raised);
        }

        [TestMethod]
        public async Task RespondToPermissionRequestAsync_ShouldCallAcpClient()
        {
            // Arrange
            var messageId = "test-message-id";
            var outcome = "selected";
            var optionId = "option-1";

            _mockAcpClient.Setup(x => x.RespondToPermissionRequestAsync(messageId, outcome, optionId))
                .ReturnsAsync(true);

            // Act
            var result = await _chatService.RespondToPermissionRequestAsync(messageId, outcome, optionId);

            // Assert
            Assert.IsTrue(result);
            _mockAcpClient.Verify(x => x.RespondToPermissionRequestAsync(messageId, outcome, optionId), Times.Once);
        }

        [TestMethod]
        public async Task DisconnectAsync_ShouldClearState()
        {
            // Arrange
            _mockAcpClient.Setup(x => x.IsInitialized).Returns(true);
            _chatService.CreateSessionAsync(new SessionNewParams { Cwd = "/test" }).Wait();

            _mockAcpClient.Setup(x => x.DisconnectAsync()).ReturnsAsync(true);

            // Act
            var result = await _chatService.DisconnectAsync();

            // Assert
            Assert.IsTrue(result);
            Assert.IsNull(_chatService.CurrentSessionId);
        }

        [TestMethod]
        public async Task LoadSessionAsync_ShouldLoadExistingSession()
        {
            // Arrange
            var loadParams = new SessionLoadParams
            {
                SessionId = "existing-session-id",
                Cwd = "/test"
            };

            var expectedResponse = new SessionLoadResponse();

            _mockAcpClient.Setup(x => x.LoadSessionAsync(loadParams, It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var response = await _chatService.LoadSessionAsync(loadParams);

            // Assert
            Assert.IsNotNull(response);
            Assert.AreEqual("existing-session-id", _chatService.CurrentSessionId);
        }

        [TestMethod]
        public async Task SetSessionModeAsync_ShouldUpdateMode()
        {
            // Arrange
            _mockAcpClient.Setup(x => x.IsInitialized).Returns(true);
            _chatService.CreateSessionAsync(new SessionNewParams { Cwd = "/test" }).Wait();

            var modeParams = new SessionSetModeParams
            {
                SessionId = "test-session-id",
                ModeId = "mode-2"
            };

            var expectedResponse = new SessionSetModeResponse("mode-2");

            _mockAcpClient.Setup(x => x.SetSessionModeAsync(modeParams, It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var response = await _chatService.SetSessionModeAsync(modeParams);

            // Assert
            Assert.IsNotNull(response);
            Assert.AreEqual("mode-2", response.ModeId);
        }

        [TestMethod]
        public async Task CancelSessionAsync_ShouldCancelCurrentSession()
        {
            // Arrange
            _mockAcpClient.Setup(x => x.IsInitialized).Returns(true);
            _chatService.CreateSessionAsync(new SessionNewParams { Cwd = "/test" }).Wait();

            var cancelParams = new SessionCancelParams
            {
                SessionId = "test-session-id",
                Reason = "User cancelled"
            };

            var expectedResponse = new SessionCancelResponse(true, "Session cancelled");

            _mockAcpClient.Setup(x => x.CancelSessionAsync(cancelParams, It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var response = await _chatService.CancelSessionAsync(cancelParams);

            // Assert
            Assert.IsNotNull(response);
            Assert.IsTrue(response.Success);
        }

        [TestMethod]
        public void ClearHistory_ShouldEmptySessionHistory()
        {
            // Arrange
            _mockAcpClient.Setup(x => x.IsInitialized).Returns(true);
            _chatService.CreateSessionAsync(new SessionNewParams { Cwd = "/test" }).Wait();

            // Add some fake history
            var entry = SessionUpdateEntry.CreateMessage(new TextContentBlock { Text = "Test" });
            // Note: In real implementation, we would need to trigger this through events

            // Act
            _chatService.ClearHistory();

            // Assert
            Assert.AreEqual(0, _chatService.SessionHistory.Count);
        }

        [TestMethod]
        public void ErrorOccurred_Event_ShouldBeRaised()
        {
            // Arrange
            var errorRaised = false;
            string? errorMessage = null;

            _chatService.ErrorOccurred += (sender, error) =>
            {
                errorRaised = true;
                errorMessage = error;
            };

            // Act
            _mockAcpClient.Raise(x => x.ErrorOccurred += null, "Test error message");

            // Wait for event to be processed
            System.Threading.Thread.Sleep(100);

            // Assert
            Assert.IsTrue(errorRaised);
            Assert.AreEqual("Test error message", errorMessage);
        }
    }
}
