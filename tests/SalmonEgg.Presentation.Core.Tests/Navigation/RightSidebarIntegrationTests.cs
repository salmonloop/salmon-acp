using Moq;
using SalmonEgg.Domain.Interfaces;
using SalmonEgg.Presentation.Services;
using SalmonEgg.Presentation.ViewModels.Navigation;
using SalmonEgg.Presentation.ViewModels.Chat;
using Microsoft.Extensions.Logging;
using Xunit;
using System.Threading;
using System.Threading.Tasks;

using SalmonEgg.Domain.Models;
using SalmonEgg.Domain.Models.Session;
using SalmonEgg.Domain.Services;
using SalmonEgg.Application.Services.Chat;
using SalmonEgg.Presentation.ViewModels.Settings;
using SalmonEgg.Presentation.Core.Mvux.Chat;
using Uno.Extensions.Reactive;
using SalmonEgg.Domain.Models.Conversation;

namespace SalmonEgg.Presentation.Core.Tests.Navigation;

public sealed class RightSidebarIntegrationTests
{
    [Fact]
    public void RightPanelMode_ReflectsServiceState()
    {
        var navState = new Mock<INavigationStateService>();
        var rightPanelService = new RightPanelService();
        using var nav = CreateNav(navState.Object, rightPanelService);

        // Act
        rightPanelService.CurrentMode = RightPanelMode.Todo;

        // Assert
        Assert.Equal(RightPanelMode.Todo, nav.RightPanelMode);
    }

    [Fact]
    public void RightPanelMode_NotifiesOnServiceChange()
    {
        var navState = new Mock<INavigationStateService>();
        var rightPanelService = new RightPanelService();
        using var nav = CreateNav(navState.Object, rightPanelService);

        bool notified = false;
        nav.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(nav.RightPanelMode))
                notified = true;
        };

        // Act
        rightPanelService.CurrentMode = RightPanelMode.Diff;

        // Assert
        Assert.True(notified);
    }

    private static MainNavigationViewModel CreateNav(INavigationStateService navState, IRightPanelService rightPanelService)
    {
        var originalContext = SynchronizationContext.Current;
        var syncContext = new SynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(syncContext);
        try
        {
            var appSettings = new Mock<IAppSettingsService>();
            appSettings.Setup(s => s.LoadAsync()).ReturnsAsync(new AppSettings());

            var appStartup = new Mock<IAppStartupService>();
            appStartup.Setup(s => s.GetLaunchOnStartupAsync()).ReturnsAsync((bool?)null);

            var appLanguage = new Mock<IAppLanguageService>();
            appLanguage.Setup(s => s.ApplyLanguageOverrideAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

            var capabilities = new Mock<IPlatformCapabilityService>();
            capabilities.SetupGet(s => s.SupportsLaunchOnStartup).Returns(false);
            capabilities.SetupGet(s => s.SupportsTray).Returns(false);
            capabilities.SetupGet(s => s.SupportsLanguageOverride).Returns(false);
            capabilities.SetupGet(s => s.SupportsMiniWindow).Returns(false);

            var uiRuntime = new Mock<IUiRuntimeService>();
            var prefLogger = new Mock<ILogger<AppPreferencesViewModel>>();

            var preferences = new AppPreferencesViewModel(
                appSettings.Object,
                appStartup.Object,
                appLanguage.Object,
                capabilities.Object,
                uiRuntime.Object,
                prefLogger.Object);

            var configService = new Mock<IConfigurationService>();
            configService.Setup(s => s.ListConfigurationsAsync()).ReturnsAsync([]);

            var acpProfilesLogger = new Mock<ILogger<AcpProfilesViewModel>>();
            var acpProfiles = new AcpProfilesViewModel(configService.Object, preferences, acpProfilesLogger.Object);

            var sessionManager = new Mock<ISessionManager>();
            var transportFactory = new Mock<ITransportFactory>();
            var messageParser = new Mock<IMessageParser>();
            var messageValidator = new Mock<IMessageValidator>();
            var errorLogger = new Mock<IErrorLogger>();
            var capabilityManager = new Mock<ICapabilityManager>();
            var serilog = new Mock<Serilog.ILogger>();

            var chatServiceFactory = new ChatServiceFactory(
                transportFactory.Object,
                messageParser.Object,
                messageValidator.Object,
                errorLogger.Object,
                capabilityManager.Object,
                sessionManager.Object,
                serilog.Object);

            var state = State.Value(new object(), () => ChatState.Empty);
            var chatStore = new ChatStore(state);

            var conversationStore = new Mock<IConversationStore>();
            conversationStore.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ConversationDocument());

            var miniWindow = new Mock<IMiniWindowCoordinator>();
            var chatLogger = new Mock<ILogger<ChatViewModel>>();
            var chatVm = new ChatViewModel(
                chatStore,
                chatServiceFactory,
                configService.Object,
                preferences,
                acpProfiles,
                sessionManager.Object,
                conversationStore.Object,
                miniWindow.Object,
                chatLogger.Object,
                syncContext);

            var ui = new Mock<IUiInteractionService>();
            var shellNavigation = new Mock<IShellNavigationService>();
            var logger = new Mock<ILogger<MainNavigationViewModel>>();

            return new MainNavigationViewModel(
                chatVm,
                sessionManager.Object,
                preferences,
                ui.Object,
                shellNavigation.Object,
                logger.Object,
                navState,
                rightPanelService);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(originalContext);
        }
    }
}
