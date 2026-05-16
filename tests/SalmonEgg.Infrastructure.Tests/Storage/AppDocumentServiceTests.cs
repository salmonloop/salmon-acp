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
    private readonly Mock<IAppDataService> _mockPaths;
    private readonly AppDocumentService _service;

    public AppDocumentServiceTests()
    {
        _mockPaths = new Mock<IAppDataService>();
        // Using forward slashes for cross-platform compatibility
        _mockPaths.Setup(p => p.AppDataRootPath).Returns("/mock/app/data");
        _service = new AppDocumentService(_mockPaths.Object);
    }

    [Fact]
    public void Constructor_NullPaths_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AppDocumentService(null!));
    }

    [Fact]
    public void DocsRootPath_ReturnsCorrectPath()
    {
        var result = _service.DocsRootPath;
        var expected = Path.Combine("/mock/app/data", "docs");
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetPrivacyPolicyPath_ReturnsCorrectPath()
    {
        var result = _service.GetPrivacyPolicyPath();
        var expected = Path.Combine("/mock/app/data", "docs", "privacy-policy.md");
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetReleaseNotesPath_ReturnsCorrectPath()
    {
        var result = _service.GetReleaseNotesPath();
        var expected = Path.Combine("/mock/app/data", "docs", "release-notes.md");
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task ExistsAsync_FileExists_ReturnsTrue()
    {
        // This test requires a real file.
        var tempFile = Path.GetTempFileName();
        try
        {
            var result = await _service.ExistsAsync(tempFile);
            Assert.True(result);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task ExistsAsync_FileDoesNotExist_ReturnsFalse()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var result = await _service.ExistsAsync(nonExistentPath);
        Assert.False(result);
    }

    [Fact]
    public async Task ExistsAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => _service.ExistsAsync("dummy/path", cts.Token));
    }
}
