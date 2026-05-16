using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;
using SalmonEgg.Application.Services.Chat;
using SalmonEgg.Domain.Models.Protocol;
using SalmonEgg.Domain.Services;

namespace SalmonEgg.Application.Tests.Services.Chat;

public sealed class DelayedLoadChatServiceTests
{
    private readonly Mock<IChatService> _mockInnerChatService;

    public DelayedLoadChatServiceTests()
    {
        _mockInnerChatService = new Mock<IChatService>(MockBehavior.Strict);
    }

    [Fact]
    public void Constructor_NullInnerService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>("inner", () => new DelayedLoadChatService(null!, TimeSpan.FromSeconds(1)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_InvalidDelay_ThrowsArgumentOutOfRangeException(int delaySeconds)
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>("loadSessionDelay", () => new DelayedLoadChatService(_mockInnerChatService.Object, TimeSpan.FromSeconds(delaySeconds)));
    }

    [Fact]
    public async Task LoadSessionAsync_AppliesDelayAndDelegates()
    {
        // Arrange
        var expectedDelay = TimeSpan.FromMilliseconds(50);
        var service = new DelayedLoadChatService(_mockInnerChatService.Object, expectedDelay);
        var loadParams = new SessionLoadParams(Guid.NewGuid().ToString(), "/tmp", null);
        var expectedResponse = new SessionLoadResponse();
        var cancellationToken = new CancellationToken();

        _mockInnerChatService
            .Setup(x => x.LoadSessionAsync(loadParams, cancellationToken))
            .ReturnsAsync(expectedResponse);

        // Act
        var startTime = DateTime.UtcNow;
        var result = await service.LoadSessionAsync(loadParams, cancellationToken);
        var elapsedTime = DateTime.UtcNow - startTime;

        // Assert
        Assert.Same(expectedResponse, result);
        Assert.True(elapsedTime >= expectedDelay);
        _mockInnerChatService.Verify(x => x.LoadSessionAsync(loadParams, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task ResumeSessionAsync_AppliesDelayAndDelegates()
    {
        // Arrange
        var expectedDelay = TimeSpan.FromMilliseconds(50);
        var service = new DelayedLoadChatService(_mockInnerChatService.Object, expectedDelay);
        var resumeParams = new SessionResumeParams(Guid.NewGuid().ToString(), "/tmp", null);
        var expectedResponse = new SessionResumeResponse();
        var cancellationToken = new CancellationToken();

        _mockInnerChatService
            .Setup(x => x.ResumeSessionAsync(resumeParams, cancellationToken))
            .ReturnsAsync(expectedResponse);

        // Act
        var startTime = DateTime.UtcNow;
        var result = await service.ResumeSessionAsync(resumeParams, cancellationToken);
        var elapsedTime = DateTime.UtcNow - startTime;

        // Assert
        Assert.Same(expectedResponse, result);
        Assert.True(elapsedTime >= expectedDelay);
        _mockInnerChatService.Verify(x => x.ResumeSessionAsync(resumeParams, cancellationToken), Times.Once);
    }

    [Fact]
    public void Properties_DelegateToInnerService()
    {
        // Arrange
        var service = new DelayedLoadChatService(_mockInnerChatService.Object, TimeSpan.FromSeconds(1));

        _mockInnerChatService.Setup(x => x.CurrentSessionId).Returns("session-123");
        _mockInnerChatService.Setup(x => x.IsInitialized).Returns(true);
        _mockInnerChatService.Setup(x => x.IsConnected).Returns(false);

        // Act & Assert
        Assert.Equal("session-123", service.CurrentSessionId);
        Assert.True(service.IsInitialized);
        Assert.False(service.IsConnected);

        _mockInnerChatService.Verify(x => x.CurrentSessionId, Times.Once);
        _mockInnerChatService.Verify(x => x.IsInitialized, Times.Once);
        _mockInnerChatService.Verify(x => x.IsConnected, Times.Once);
    }
}
