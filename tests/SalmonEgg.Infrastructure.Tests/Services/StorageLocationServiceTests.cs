using System;
using System.IO;
using System.Threading.Tasks;
using Moq;
using SalmonEgg.Domain.Services;
using SalmonEgg.Infrastructure.Services;
using Xunit;

namespace SalmonEgg.Infrastructure.Tests.Services;

public sealed class StorageLocationServiceTests : IDisposable
{
    private readonly string _root;

    public StorageLocationServiceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "SalmonEggStorageLocationTests", Guid.NewGuid().ToString("N"));
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }
        catch
        {
        }
    }

    [Fact]
    public async Task OpenAsync_CreatesKnownAppLocationBeforeOpening()
    {
        var paths = new Mock<IAppDataService>();
        paths.SetupGet(service => service.ExportsDirectoryPath).Returns(Path.Combine(_root, "exports"));
        var capabilities = new Mock<IPlatformCapabilityService>();
        capabilities.SetupGet(service => service.SupportsExternalFileOpen).Returns(true);
        var shell = new Mock<IPlatformShellService>();
        shell.Setup(service => service.OpenFolderAsync(It.IsAny<string>())).ReturnsAsync(true);
        var sut = new StorageLocationService(paths.Object, capabilities.Object, shell.Object);

        var opened = await sut.OpenAsync(AppStorageLocation.Exports);

        var path = paths.Object.ExportsDirectoryPath;
        Assert.True(opened);
        Assert.True(Directory.Exists(path));
        shell.Verify(service => service.OpenFolderAsync(path), Times.Once);
    }

    [Fact]
    public async Task OpenAsync_WhenExternalOpenUnsupported_DoesNotCreateOrOpen()
    {
        var path = Path.Combine(_root, "exports");
        var paths = new Mock<IAppDataService>();
        paths.SetupGet(service => service.ExportsDirectoryPath).Returns(path);
        var capabilities = new Mock<IPlatformCapabilityService>();
        capabilities.SetupGet(service => service.SupportsExternalFileOpen).Returns(false);
        var shell = new Mock<IPlatformShellService>();
        var sut = new StorageLocationService(paths.Object, capabilities.Object, shell.Object);

        var opened = await sut.OpenAsync(AppStorageLocation.Exports);

        Assert.False(opened);
        Assert.False(Directory.Exists(path));
        shell.Verify(service => service.OpenFolderAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task OpenExistingFolderAsync_WhenFolderDoesNotExist_DoesNotCreateOrOpen()
    {
        var paths = new Mock<IAppDataService>();
        var capabilities = new Mock<IPlatformCapabilityService>();
        capabilities.SetupGet(service => service.SupportsExternalFileOpen).Returns(true);
        var shell = new Mock<IPlatformShellService>();
        var sut = new StorageLocationService(paths.Object, capabilities.Object, shell.Object);
        var path = Path.Combine(_root, "docs");

        var opened = await sut.OpenExistingFolderAsync(path);

        Assert.False(opened);
        Assert.False(Directory.Exists(path));
        shell.Verify(service => service.OpenFolderAsync(It.IsAny<string>()), Times.Never);
    }
}
