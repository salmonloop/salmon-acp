using Microsoft.Extensions.Logging;
using Moq;
using SalmonEgg.Application.Services.Chat;
using SalmonEgg.Domain.Interfaces.Transport;
using SalmonEgg.Domain.Models;
using SalmonEgg.Domain.Services;
using SalmonEgg.Presentation.Core.Mvux.Chat;
using SalmonEgg.Presentation.Core.Services.Chat;
using SalmonEgg.Presentation.ViewModels.Chat;
using SalmonEgg.Presentation.ViewModels.Settings;
using Serilog;
using Uno.Extensions.Reactive;
using Xunit;

namespace SalmonEgg.Presentation.Core.Tests.Settings;

public sealed class DiagnosticsSettingsViewModelTests
{
    private static ChatViewModel CreateChatViewModel()
    {
        var chatState = State.Value(new object(), () => ChatState.Empty);
        var connectionState = State.Value(new object(), () => ChatConnectionState.Empty);
        var chatStore = new ChatStore(chatState);
        var connectionStore = new ChatConnectionStore(connectionState);

        var transportFactory = new Mock<ITransportFactory>();
        var parser = new Mock<IMessageParser>();
        var validator = new Mock<IMessageValidator>();
        var errorLogger = new Mock<IErrorLogger>();
        var sessionManager = new Mock<ISessionManager>();
        var serilogLogger = new Mock<ILogger>();

        var chatServiceFactory = new ChatServiceFactory(
            transportFactory.Object,
            parser.Object,
            validator.Object,
            errorLogger.Object,
            sessionManager.Object,
            serilogLogger.Object);

        var appSettingsService = new Mock<IAppSettingsService>();
        appSettingsService.Setup(service => service.LoadAsync())
            .ReturnsAsync(new AppSettings());
        var startupService = new Mock<IAppStartupService>();
        startupService.SetupGet(service => service.IsSupported).Returns(false);
        startupService.Setup(service => service.GetLaunchOnStartupAsync()).ReturnsAsync((bool?)null);
        var languageService = new Mock<IAppLanguageService>();
        var capabilityService = new Mock<IPlatformCapabilityService>();
        capabilityService.SetupGet(service => service.SupportsLaunchOnStartup).Returns(false);
        capabilityService.SetupGet(service => service.SupportsTray).Returns(false);
        capabilityService.SetupGet(service => service.SupportsLanguageOverride).Returns(false);
        capabilityService.SetupGet(service => service.SupportsMiniWindow).Returns(false);
        var uiRuntime = new Mock<IUiRuntimeService>();

        var preferences = new AppPreferencesViewModel(
            appSettingsService.Object,
            startupService.Object,
            languageService.Object,
            capabilityService.Object,
            uiRuntime.Object,
            Mock.Of<Microsoft.Extensions.Logging.ILogger<AppPreferencesViewModel>>());

        var configurationService = new Mock<IConfigurationService>();
        var profiles = new AcpProfilesViewModel(
            configurationService.Object,
            preferences,
            Mock.Of<Microsoft.Extensions.Logging.ILogger<AcpProfilesViewModel>>());
        var miniWindowCoordinator = new Mock<IMiniWindowCoordinator>();
        var conversationStore = new Mock<IConversationStore>();
        conversationStore.Setup(store => store.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Domain.Models.Conversation.ConversationDocument());
        var workspace = new ChatConversationWorkspace(
            sessionManager.Object,
            conversationStore.Object,
            new AppPreferencesConversationWorkspacePreferences(preferences),
            Mock.Of<Microsoft.Extensions.Logging.ILogger<ChatConversationWorkspace>>());
        var presenter = new ConversationCatalogPresenter();

        return new ChatViewModel(
            chatStore,
            chatServiceFactory,
            configurationService.Object,
            preferences,
            profiles,
            sessionManager.Object,
            miniWindowCoordinator.Object,
            workspace,
            presenter,
            new ChatStateProjector(),
            new AcpSessionUpdateProjector(),
            connectionStore,
            Mock.Of<Microsoft.Extensions.Logging.ILogger<ChatViewModel>>());
    }

    [Fact]
    public void Constructor_ComposesLiveLogViewer()
    {
        using var chat = CreateChatViewModel();
        var paths = new Mock<IAppDataService>();
        paths.SetupGet(p => p.AppDataRootPath).Returns("C:/app");
        paths.SetupGet(p => p.LogsDirectoryPath).Returns("C:/app/logs");
        var bundle = new Mock<IDiagnosticsBundleService>();
        var shell = new Mock<IPlatformShellService>();
        var service = new Mock<ILiveLogStreamService>();
        var liveLogger = new Mock<ILogger<LiveLogViewerViewModel>>();
        var diagnosticsLogger = new Mock<ILogger<DiagnosticsSettingsViewModel>>();
        var liveLogViewer = new LiveLogViewerViewModel(service.Object, paths.Object.LogsDirectoryPath, liveLogger.Object);

        var viewModel = new DiagnosticsSettingsViewModel(
            chat,
            paths.Object,
            bundle.Object,
            shell.Object,
            liveLogViewer,
            diagnosticsLogger.Object);

        Assert.Same(liveLogViewer, viewModel.LiveLogViewer);
    }
}
