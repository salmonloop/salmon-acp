using System.Runtime.CompilerServices;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Moq;
using SalmonEgg.Domain.Services;
using SalmonEgg.Presentation.Core.Tests.Localization;
using SalmonEgg.Presentation.Core.Resources;
using SalmonEgg.Presentation.Core.Tests.Threading;
using SalmonEgg.Presentation.ViewModels.Chat;
using SalmonEgg.Presentation.ViewModels.Settings;
using Xunit;

namespace SalmonEgg.Presentation.Core.Tests.Settings;

public sealed class DiagnosticsSettingsViewModelTests
{
    [Fact]
    public void Constructor_ComposesLiveLogViewer()
    {
        var chat = (ChatViewModel)RuntimeHelpers.GetUninitializedObject(typeof(ChatViewModel));
        var paths = new Mock<IAppDataService>();
        paths.SetupGet(p => p.AppDataRootPath).Returns("C:/app");
        paths.SetupGet(p => p.LogsDirectoryPath).Returns("C:/app/logs");
        var bundle = new Mock<IDiagnosticsBundleService>();
        var shell = new Mock<IPlatformShellService>();
        var storageLocations = new Mock<IStorageLocationService>();
        var logFileCatalog = new Mock<ILogFileCatalog>();
        var service = new Mock<ILiveLogStreamService>();
        var liveLogger = new Mock<ILogger<LiveLogViewerViewModel>>();
        var diagnosticsLogger = new Mock<ILogger<DiagnosticsSettingsViewModel>>();
        var liveLogViewer = new LiveLogViewerViewModel(
            service.Object,
            paths.Object.LogsDirectoryPath,
            liveLogger.Object,
            new ImmediateUiDispatcher(),
            new TestCoreStringLocalizer());

        var viewModel = new DiagnosticsSettingsViewModel(
            chat,
            paths.Object,
            bundle.Object,
            shell.Object,
            storageLocations.Object,
            logFileCatalog.Object,
            liveLogViewer,
            Mock.Of<IStringLocalizer<CoreStrings>>(),
            diagnosticsLogger.Object);

        Assert.Same(liveLogViewer, viewModel.LiveLogViewer);
    }
}
