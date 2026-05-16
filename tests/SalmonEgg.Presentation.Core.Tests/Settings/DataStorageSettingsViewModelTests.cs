using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using SalmonEgg.Domain.Models.Diagnostics;
using SalmonEgg.Domain.Services;
using SalmonEgg.Presentation.Core.Tests.Localization;
using SalmonEgg.Presentation.Services;
using SalmonEgg.Presentation.ViewModels.Chat;
using SalmonEgg.Presentation.ViewModels.Settings;
using Xunit;

namespace SalmonEgg.Presentation.Core.Tests.Settings;

public sealed class DataStorageSettingsViewModelTests
{
    [Fact]
    public async Task ExportCurrentSessionJsonCommand_WhenLocalFileExportUnsupported_DoesNotExport()
    {
        var sessionExport = new Mock<ISessionExportService>();
        var ui = new Mock<IUiInteractionService>();
        var viewModel = CreateViewModel(
            supportsLocalFileExport: false,
            sessionExport: sessionExport,
            ui: ui);

        await viewModel.ExportCurrentSessionJsonCommand.ExecuteAsync(null);

        sessionExport.Verify(service => service.ExportAsync(It.IsAny<SessionExportRequest>(), default), Times.Never);
        ui.Verify(service => service.ShowInfoAsync("当前平台暂不支持导出本地文件。"), Times.Once);
    }

    [Fact]
    public async Task CreateDiagnosticsBundleCommand_WhenLocalFileExportUnsupported_DoesNotCreateBundle()
    {
        var diagnostics = new Mock<IDiagnosticsBundleService>();
        var ui = new Mock<IUiInteractionService>();
        var viewModel = CreateViewModel(
            supportsLocalFileExport: false,
            diagnostics: diagnostics,
            ui: ui);

        await viewModel.CreateDiagnosticsBundleCommand.ExecuteAsync(null);

        diagnostics.Verify(service => service.CreateBundleAsync(It.IsAny<DiagnosticsSnapshot>()), Times.Never);
        ui.Verify(service => service.ShowInfoAsync("当前平台暂不支持导出本地文件。"), Times.Once);
    }


    [Fact]
    public void Properties_MapToAppDataService()
    {
        var paths = new Mock<IAppDataService>();
        paths.SetupGet(p => p.AppDataRootPath).Returns("/app/data");
        paths.SetupGet(p => p.LogsDirectoryPath).Returns("/app/logs");
        paths.SetupGet(p => p.CacheRootPath).Returns("/app/cache");
        paths.SetupGet(p => p.ExportsDirectoryPath).Returns("/app/exports");

        var viewModel = CreateViewModel(paths: paths);

        Assert.Equal("/app/data", viewModel.AppDataRootPath);
        Assert.Equal("/app/logs", viewModel.LogsDirectoryPath);
        Assert.Equal("/app/cache", viewModel.CacheRootPath);
        Assert.Equal("/app/exports", viewModel.ExportsDirectoryPath);
    }

    [Fact]
    public void CapabilitiesProperties_MapToPlatformCapabilityService()
    {
        var viewModel = CreateViewModel(supportsExternalFileOpen: true, supportsLocalFileExport: false);
        Assert.True(viewModel.CanOpenExternalFiles);
        Assert.False(viewModel.CanExportLocalFiles);
    }

    [Theory]
    [InlineData(AppStorageLocation.AppData)]
    [InlineData(AppStorageLocation.Cache)]
    [InlineData(AppStorageLocation.Logs)]
    [InlineData(AppStorageLocation.Exports)]
    public async Task OpenFolderCommands_WhenSupported_OpensStorageLocation(AppStorageLocation location)
    {
        var storageLocations = new Mock<IStorageLocationService>();
        storageLocations.Setup(s => s.OpenAsync(location)).ReturnsAsync(true);
        var viewModel = CreateViewModel(storageLocations: storageLocations);

        if (location == AppStorageLocation.AppData) await viewModel.OpenAppDataFolderCommand.ExecuteAsync(null);
        else if (location == AppStorageLocation.Cache) await viewModel.OpenCacheFolderCommand.ExecuteAsync(null);
        else if (location == AppStorageLocation.Logs) await viewModel.OpenLogsFolderCommand.ExecuteAsync(null);
        else if (location == AppStorageLocation.Exports) await viewModel.OpenExportsFolderCommand.ExecuteAsync(null);

        storageLocations.Verify(s => s.OpenAsync(location), Times.Once);
    }

    [Theory]
    [InlineData(AppStorageLocation.AppData)]
    [InlineData(AppStorageLocation.Cache)]
    [InlineData(AppStorageLocation.Logs)]
    [InlineData(AppStorageLocation.Exports)]
    public async Task OpenFolderCommands_WhenUnsupported_ShowsInfoNotification(AppStorageLocation location)
    {
        var storageLocations = new Mock<IStorageLocationService>();
        storageLocations.Setup(s => s.OpenAsync(location)).ReturnsAsync(false);
        var ui = new Mock<IUiInteractionService>();
        var viewModel = CreateViewModel(storageLocations: storageLocations, ui: ui);

        if (location == AppStorageLocation.AppData) await viewModel.OpenAppDataFolderCommand.ExecuteAsync(null);
        else if (location == AppStorageLocation.Cache) await viewModel.OpenCacheFolderCommand.ExecuteAsync(null);
        else if (location == AppStorageLocation.Logs) await viewModel.OpenLogsFolderCommand.ExecuteAsync(null);
        else if (location == AppStorageLocation.Exports) await viewModel.OpenExportsFolderCommand.ExecuteAsync(null);

        storageLocations.Verify(s => s.OpenAsync(location), Times.Once);
        ui.Verify(service => service.ShowInfoAsync("当前平台暂不支持打开本地文件或目录。"), Times.Once);
    }

    [Fact]
    public async Task ClearCacheCommand_CallsMaintenanceService()
    {
        var maintenance = new Mock<IAppMaintenanceService>();
        var viewModel = CreateViewModel(maintenance: maintenance);

        await viewModel.ClearCacheCommand.ExecuteAsync(null);

        maintenance.Verify(s => s.ClearCacheAsync(), Times.Once);
    }

    [Fact]
    public async Task ClearAllLocalDataCommand_CallsMaintenanceService()
    {
        var maintenance = new Mock<IAppMaintenanceService>();
        var viewModel = CreateViewModel(maintenance: maintenance);

        await viewModel.ClearAllLocalDataCommand.ExecuteAsync(null);

        maintenance.Verify(s => s.ClearAllLocalDataAsync(), Times.Once);
    }

    private static DataStorageSettingsViewModel CreateViewModel(
        bool supportsLocalFileExport = true,
        bool supportsExternalFileOpen = true,
        Mock<IAppDataService>? paths = null,
        Mock<IAppMaintenanceService>? maintenance = null,
        Mock<IDiagnosticsBundleService>? diagnostics = null,
        Mock<IPlatformShellService>? shell = null,
        Mock<IStorageLocationService>? storageLocations = null,
        Mock<ISessionExportService>? sessionExport = null,
        Mock<IUiInteractionService>? ui = null)
    {
        var preferences = (AppPreferencesViewModel)RuntimeHelpers.GetUninitializedObject(typeof(AppPreferencesViewModel));
        var chat = (ChatViewModel)RuntimeHelpers.GetUninitializedObject(typeof(ChatViewModel));
        var capabilities = new Mock<IPlatformCapabilityService>();
        capabilities.SetupGet(service => service.SupportsExternalFileOpen).Returns(supportsExternalFileOpen);
        capabilities.SetupGet(service => service.SupportsLocalFileExport).Returns(supportsLocalFileExport);

        return new DataStorageSettingsViewModel(
            preferences,
            chat,
            paths?.Object ?? Mock.Of<IAppDataService>(),
            maintenance?.Object ?? Mock.Of<IAppMaintenanceService>(),
            diagnostics?.Object ?? Mock.Of<IDiagnosticsBundleService>(),
            shell?.Object ?? Mock.Of<IPlatformShellService>(),
            capabilities.Object,
            storageLocations?.Object ?? Mock.Of<IStorageLocationService>(),
            sessionExport?.Object ?? Mock.Of<ISessionExportService>(),
            ui?.Object ?? Mock.Of<IUiInteractionService>(),
            new TestCoreStringLocalizer(),
            Mock.Of<ILogger<DataStorageSettingsViewModel>>());
    }
}
