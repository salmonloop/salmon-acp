using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using SalmonEgg.Domain.Services;
using SalmonEgg.Infrastructure.Storage;
using Xunit;

namespace SalmonEgg.Infrastructure.Tests.Storage;

public class AppDocumentServiceTests
{
    private readonly Mock<IAppDataService> _mockAppDataService;
    private readonly AppDocumentService _sut;
    private readonly string _testAppDataRootPath;

    public AppDocumentServiceTests()
    {
        _mockAppDataService = new Mock<IAppDataService>();
        // Use forward slashes for tests as per memory guidelines (Unix paths expected in CI)
        _testAppDataRootPath = "/mock/app/data/root";
        _mockAppDataService.Setup(x => x.AppDataRootPath).Returns(_testAppDataRootPath);

        _sut = new AppDocumentService(_mockAppDataService.Object);
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenAppDataServiceIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AppDocumentService(null!));
    }

    [Fact]
    public void DocsRootPath_ReturnsCorrectPath()
    {
        // Arrange
        var expectedPath = Path.Combine(_testAppDataRootPath, "docs");

        // Act
        var actualPath = _sut.DocsRootPath;

        // Assert
        Assert.Equal(expectedPath, actualPath);
    }

    [Fact]
    public void GetPrivacyPolicyPath_ReturnsCorrectPath()
    {
        // Arrange
        var expectedPath = Path.Combine(_testAppDataRootPath, "docs", "privacy-policy.md");

        // Act
        var actualPath = _sut.GetPrivacyPolicyPath();

        // Assert
        Assert.Equal(expectedPath, actualPath);
    }

    [Fact]
    public void GetReleaseNotesPath_ReturnsCorrectPath()
    {
        // Arrange
        var expectedPath = Path.Combine(_testAppDataRootPath, "docs", "release-notes.md");

        // Act
        var actualPath = _sut.GetReleaseNotesPath();

        // Assert
        Assert.Equal(expectedPath, actualPath);
    }

    [Fact]
    public async Task ExistsAsync_ThrowsOperationCanceledException_WhenCancellationRequested()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => _sut.ExistsAsync("any-path.md", cts.Token));
    }

    [Fact]
    public async Task ExistsAsync_ReturnsFalse_WhenFileDoesNotExist()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "non-existent-file.md");

        // Act
        var exists = await _sut.ExistsAsync(nonExistentPath);

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task ExistsAsync_ReturnsTrue_WhenFileExists()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            // Act
            var exists = await _sut.ExistsAsync(tempFile);

            // Assert
            Assert.True(exists);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
