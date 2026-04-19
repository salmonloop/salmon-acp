using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Xunit;
using SalmonEgg.Application.Services.Chat;
using SalmonEgg.Domain.Models.Session;
using SalmonEgg.Domain.Services;
using SalmonEgg.Domain.Services.Security;
using SalmonEgg.Domain.Models.Protocol;

namespace SalmonEgg.Application.Tests.Services.Chat
{
    public class ErrorRecoveryServiceTests
    {
        private readonly Mock<IChatService> _chatServiceMock;
        private readonly Mock<IPathValidator> _pathValidatorMock;
        private readonly Mock<IErrorLogger> _errorLoggerMock;
        private readonly ErrorRecoveryService _sut;

        public ErrorRecoveryServiceTests()
        {
            _chatServiceMock = new Mock<IChatService>();
            _pathValidatorMock = new Mock<IPathValidator>();
            _errorLoggerMock = new Mock<IErrorLogger>();

            _sut = new ErrorRecoveryService(
                _chatServiceMock.Object,
                _pathValidatorMock.Object,
                _errorLoggerMock.Object);
        }

        [Fact]
        public async Task RecoverFromConnectionErrorAsync_WhenAutoReconnectEnabled_ReturnsSuccessOnFirstAttempt()
        {
            // Arrange
            _sut.SetConfig(new ErrorRecoveryConfig
            {
                EnableAutoReconnect = true,
                MaxRetries = 3,
                InitialDelayMs = 1 // Use small delay for faster tests
            });

            // Act
            var result = await _sut.RecoverFromConnectionErrorAsync("test error");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(1, _sut.GetCurrentRetryCount());
            _errorLoggerMock.Verify(x => x.LogError(It.IsAny<ErrorLogEntry>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task RecoverFromConnectionErrorAsync_WhenAutoReconnectDisabled_FailsAfterMaxRetries()
        {
            // Arrange
            _sut.SetConfig(new ErrorRecoveryConfig
            {
                EnableAutoReconnect = false,
                MaxRetries = 2,
                InitialDelayMs = 1
            });

            // Act
            var result = await _sut.RecoverFromConnectionErrorAsync("test error");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("reached max retries", result.Error);
            Assert.Equal(2, _sut.GetCurrentRetryCount());
        }

        [Fact]
        public async Task RecoverFromSessionErrorAsync_WhenDisabled_ReturnsFailure()
        {
            // Arrange
            _sut.SetConfig(new ErrorRecoveryConfig { EnableSessionAutoRecovery = false });

            // Act
            var result = await _sut.RecoverFromSessionErrorAsync("session-1", "test error");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Session auto-recovery is disabled", result.Error);
        }

        [Fact]
        public async Task RecoverFromSessionErrorAsync_WhenEnabledAndCreationSucceeds_ReturnsNewSessionId()
        {
            // Arrange
            _sut.SetConfig(new ErrorRecoveryConfig { EnableSessionAutoRecovery = true });

            _chatServiceMock.Setup(x => x.CreateSessionAsync(It.IsAny<SessionNewParams>()))
                .ReturnsAsync(new SessionNewResponse { SessionId = "new-session" });

            // Act
            var result = await _sut.RecoverFromSessionErrorAsync("old-session", "test error");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("new-session", result.Value);
            _chatServiceMock.Verify(x => x.CreateSessionAsync(It.IsAny<SessionNewParams>()), Times.Once);
        }

        [Fact]
        public async Task RecoverFromSessionErrorAsync_WhenEnabledAndCreationThrows_ReturnsFailure()
        {
            // Arrange
            _sut.SetConfig(new ErrorRecoveryConfig { EnableSessionAutoRecovery = true });

            _chatServiceMock.Setup(x => x.CreateSessionAsync(It.IsAny<SessionNewParams>()))
                .ThrowsAsync(new Exception("Creation failed"));

            // Act
            var result = await _sut.RecoverFromSessionErrorAsync("old-session", "test error");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Creation failed", result.Error);
        }

        [Fact]
        public async Task RecoverFromFileSystemErrorAsync_WhenDisabled_ReturnsFailure()
        {
            // Arrange
            _sut.SetConfig(new ErrorRecoveryConfig { EnableFileSystemRecovery = false });

            // Act
            var result = await _sut.RecoverFromFileSystemErrorAsync("read", "/test/path", "test error");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("File system error recovery is disabled", result.Error);
        }

        [Fact]
        public async Task RecoverFromFileSystemErrorAsync_WhenEnabledAndPathIsValid_ReturnsSuccess()
        {
            // Arrange
            _sut.SetConfig(new ErrorRecoveryConfig { EnableFileSystemRecovery = true });
            _pathValidatorMock.Setup(x => x.ValidatePath("/test/path")).Returns(true);

            // Act
            var result = await _sut.RecoverFromFileSystemErrorAsync("read", "/test/path", "test error");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.Value);
        }

        [Fact]
        public async Task RecoverFromFileSystemErrorAsync_WhenEnabledAndPathIsInvalid_ReturnsFailure()
        {
            // Arrange
            _sut.SetConfig(new ErrorRecoveryConfig { EnableFileSystemRecovery = true });
            _pathValidatorMock.Setup(x => x.ValidatePath("/test/path")).Returns(false);
            _pathValidatorMock.Setup(x => x.GetValidationErrors("/test/path"))
                .Returns(new List<string> { "Invalid path" });

            // Act
            var result = await _sut.RecoverFromFileSystemErrorAsync("read", "/test/path", "test error");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Invalid path", result.Error);
        }

        [Fact]
        public async Task RecoverFromFileSystemErrorAsync_WhenValidationThrows_ReturnsFailure()
        {
            // Arrange
            _sut.SetConfig(new ErrorRecoveryConfig { EnableFileSystemRecovery = true });
            _pathValidatorMock.Setup(x => x.ValidatePath("/test/path"))
                .Throws(new Exception("Validation failed"));

            // Act
            var result = await _sut.RecoverFromFileSystemErrorAsync("read", "/test/path", "test error");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Validation failed", result.Error);
        }

        [Fact]
        public async Task RecoverFromProtocolVersionErrorAsync_WhenWarningEnabled_ReturnsFailure()
        {
            // Arrange
            _sut.SetConfig(new ErrorRecoveryConfig { ShowProtocolVersionWarning = true });

            // Act
            var result = await _sut.RecoverFromProtocolVersionErrorAsync(2, 1);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Protocol version incompatible", result.Error);
        }

        [Fact]
        public async Task RecoverFromProtocolVersionErrorAsync_WhenWarningDisabled_ReturnsSuccess()
        {
            // Arrange
            _sut.SetConfig(new ErrorRecoveryConfig { ShowProtocolVersionWarning = false });

            // Act
            var result = await _sut.RecoverFromProtocolVersionErrorAsync(2, 1);

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void ConfigManagement_ShouldWorkCorrectly()
        {
            // Arrange
            var config = new ErrorRecoveryConfig
            {
                EnableAutoReconnect = false,
                MaxRetries = 10,
                InitialDelayMs = 200,
                MaxDelayMs = 5000,
                DelayMultiplier = 1.5,
                EnableSessionAutoRecovery = false,
                EnableFileSystemRecovery = false,
                ShowProtocolVersionWarning = false
            };

            // Act
            _sut.SetConfig(config);
            var result = _sut.GetConfig();

            // Assert
            Assert.Equal(config.EnableAutoReconnect, result.EnableAutoReconnect);
            Assert.Equal(config.MaxRetries, result.MaxRetries);
            Assert.Equal(config.InitialDelayMs, result.InitialDelayMs);
            Assert.Equal(config.MaxDelayMs, result.MaxDelayMs);
            Assert.Equal(config.DelayMultiplier, result.DelayMultiplier);
            Assert.Equal(config.EnableSessionAutoRecovery, result.EnableSessionAutoRecovery);
            Assert.Equal(config.EnableFileSystemRecovery, result.EnableFileSystemRecovery);
            Assert.Equal(config.ShowProtocolVersionWarning, result.ShowProtocolVersionWarning);
        }

        [Fact]
        public async Task ResetRetryCount_ShouldResetToZero()
        {
            // Arrange - simulate a failure to increase retry count
            _sut.SetConfig(new ErrorRecoveryConfig { EnableAutoReconnect = false, MaxRetries = 1, InitialDelayMs = 1 });
            _ = await _sut.RecoverFromConnectionErrorAsync("test");
            Assert.Equal(1, _sut.GetCurrentRetryCount());

            // Act
            _sut.ResetRetryCount();

            // Assert
            Assert.Equal(0, _sut.GetCurrentRetryCount());
        }
    }
}
