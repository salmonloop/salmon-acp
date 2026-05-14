using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using SalmonEgg.Domain.Models.Diagnostics;
using SalmonEgg.Domain.Services;
using SalmonEgg.Presentation.Core.Tests.Localization;
using SalmonEgg.Presentation.Core.Resources;
using SalmonEgg.Presentation.Core.Tests.Threading;
using SalmonEgg.Presentation.Services;
using SalmonEgg.Presentation.ViewModels.Chat;
using SalmonEgg.Presentation.ViewModels.Settings;
using Xunit;

namespace SalmonEgg.Presentation.Core.Tests.Settings;

public sealed class DiagnosticsSettingsViewModelTests
{
    [Fact]
    public void Constructor_ComposesLiveLogViewer()
    {
        var liveLogViewer = CreateLiveLogViewer();
        var viewModel = CreateViewModel(liveLogViewer: liveLogViewer);

        Assert.Same(liveLogViewer, viewModel.LiveLogViewer);
    }

    [Fact]
    public void PlatformCapabilities_ReflectBindableAvailability()
    {
        var viewModel = CreateViewModel(supportsExternalOpen: false, supportsLocalFileExport: false);

        Assert.False(viewModel.CanOpenExternalFiles);
        Assert.False(viewModel.CanExportLocalFiles);
    }

    [Fact]
    public async Task OpenLogsFolderCommand_WhenExternalOpenFails_ShowsLocalizedMessage()
    {
        var storageLocations = new Mock<IStorageLocationService>();
        storageLocations.Setup(service => service.OpenAsync(AppStorageLocation.Logs)).ReturnsAsync(false);
        var ui = new Mock<IUiInteractionService>();
        var viewModel = CreateViewModel(
            supportsExternalOpen: false,
            storageLocations: storageLocations,
            ui: ui);

        await viewModel.OpenLogsFolderCommand.ExecuteAsync(null);

        storageLocations.Verify(service => service.OpenAsync(AppStorageLocation.Logs), Times.Once);
        ui.Verify(service => service.ShowInfoAsync("当前平台暂不支持打开本地文件或目录。"), Times.Once);
    }

    [Fact]
    public async Task CreateDiagnosticsBundleCommand_WhenLocalFileExportUnsupported_DoesNotCreateBundle()
    {
        var bundle = new Mock<IDiagnosticsBundleService>();
        var ui = new Mock<IUiInteractionService>();
        var viewModel = CreateViewModel(
            supportsLocalFileExport: false,
            bundle: bundle,
            ui: ui);

        await viewModel.CreateDiagnosticsBundleCommand.ExecuteAsync(null);

        bundle.Verify(service => service.CreateBundleAsync(It.IsAny<DiagnosticsSnapshot>()), Times.Never);
        ui.Verify(service => service.ShowInfoAsync("当前平台暂不支持导出本地文件。"), Times.Once);
    }

    private static DiagnosticsSettingsViewModel CreateViewModel(
        bool supportsExternalOpen = true,
        bool supportsLocalFileExport = true,
        Mock<IDiagnosticsBundleService>? bundle = null,
        Mock<IStorageLocationService>? storageLocations = null,
        Mock<IUiInteractionService>? ui = null,
        LiveLogViewerViewModel? liveLogViewer = null)
    {
        var chat = (ChatViewModel)RuntimeHelpers.GetUninitializedObject(typeof(ChatViewModel));
        var paths = new Mock<IAppDataService>();
        paths.SetupGet(p => p.AppDataRootPath).Returns("C:/app");
        paths.SetupGet(p => p.LogsDirectoryPath).Returns("C:/app/logs");
        var capabilities = new Mock<IPlatformCapabilityService>();
        capabilities.SetupGet(service => service.SupportsExternalFileOpen).Returns(supportsExternalOpen);
        capabilities.SetupGet(service => service.SupportsLocalFileExport).Returns(supportsLocalFileExport);

        return new DiagnosticsSettingsViewModel(
            chat,
            paths.Object,
            bundle?.Object ?? Mock.Of<IDiagnosticsBundleService>(),
            Mock.Of<IPlatformShellService>(),
            capabilities.Object,
            storageLocations?.Object ?? Mock.Of<IStorageLocationService>(),
            Mock.Of<ILogFileCatalog>(),
            ui?.Object ?? Mock.Of<IUiInteractionService>(),
            liveLogViewer ?? CreateLiveLogViewer(),
            new TestCoreStringLocalizer(),
            Mock.Of<ILogger<DiagnosticsSettingsViewModel>>());
    }

    private static LiveLogViewerViewModel CreateLiveLogViewer()
    {
        var service = new Mock<ILiveLogStreamService>();
        var paths = new Mock<IAppDataService>();
        paths.SetupGet(p => p.LogsDirectoryPath).Returns("C:/app/logs");

        return new LiveLogViewerViewModel(
            service.Object,
            paths.Object.LogsDirectoryPath,
            Mock.Of<ILogger<LiveLogViewerViewModel>>(),
            new ImmediateUiDispatcher(),
            new TestCoreStringLocalizer());
    }
}
