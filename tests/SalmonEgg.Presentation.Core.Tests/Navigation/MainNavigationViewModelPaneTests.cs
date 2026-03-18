using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using Moq;
using SalmonEgg.Domain.Models;
using SalmonEgg.Domain.Models.Conversation;
using SalmonEgg.Domain.Models.Session;
using SalmonEgg.Domain.Services;
using SalmonEgg.Domain.Interfaces;
using SalmonEgg.Presentation.Core.Mvux.Chat;
using SalmonEgg.Presentation.Core.Services;
using SalmonEgg.Presentation.Services;
using SalmonEgg.Presentation.ViewModels.Chat;
using SalmonEgg.Presentation.ViewModels.Navigation;
using SalmonEgg.Presentation.ViewModels.Settings;
using SerilogLogger = Serilog.ILogger;
using Uno.Extensions.Reactive;
using Xunit;

namespace SalmonEgg.Presentation.Core.Tests.Navigation;

[Collection("NonParallel")]
public sealed class MainNavigationViewModelPaneTests
{
    [Fact]
    public void NavigationState_IsSharedAcrossViewModels()
    {
        var originalContext = SynchronizationContext.Current;
        var syncContext = new SynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(syncContext);
        try
        {
            var navState = new FakeNavigationPaneState();

            var sessionManager = new Mock<ISessionManager>();
            var preferences = CreatePreferences();
            using var chat = CreateChatViewModel(syncContext, preferences, sessionManager.Object);
            var chatViewModel = chat.ViewModel;
            var ui = new Mock<IUiInteractionService>();
            var shellNavigation = new Mock<IShellNavigationService>();
            var navLogger = new Mock<ILogger<MainNavigationViewModel>>();
            var metricsSink = new Mock<IShellLayoutMetricsSink>();

            using var navVm = new MainNavigationViewModel(
                chatViewModel,
                sessionManager.Object,
                preferences,
                ui.Object,
                shellNavigation.Object,
                navLogger.Object,
                navState,
                metricsSink.Object);

            var startItem = new StartNavItemViewModel(navState);

            navState.SetPaneOpen(true);

            Assert.True(navVm.IsPaneOpen);
            Assert.True(startItem.IsPaneOpen);

            navState.SetPaneOpen(false);

            Assert.False(navVm.IsPaneOpen);
            Assert.False(startItem.IsPaneOpen);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(originalContext);
        }
    }

    [Fact]
    public void NavigationState_TriggersPropertyChangeNotifications()
    {
        var navState = new FakeNavigationPaneState();
        var item = new StartNavItemViewModel(navState);

        bool isPaneOpenChangedCalled = false;
        item.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(item.IsPaneOpen))
                isPaneOpenChangedCalled = true;
        };

        navState.SetPaneOpen(!navState.IsPaneOpen);

        Assert.True(isPaneOpenChangedCalled);
    }

    private sealed class FakeNavigationPaneState : INavigationPaneState
    {
        public bool IsPaneOpen { get; private set; }
        public event EventHandler? PaneStateChanged;

        public void SetPaneOpen(bool isOpen)
        {
            if (IsPaneOpen == isOpen)
            {
                return;
            }

            IsPaneOpen = isOpen;
            PaneStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private static ChatViewModelHarness CreateChatViewModel(
        SynchronizationContext syncContext,
        AppPreferencesViewModel preferences,
        ISessionManager sessionManager)
    {
        var state = Uno.Extensions.Reactive.State.Value(new object(), () => ChatState.Empty);
        var chatStore = new Mock<IChatStore>();
        chatStore.Setup(s => s.State).Returns(state);
        var transportFactory = new Mock<ITransportFactory>();
        var messageParser = new Mock<IMessageParser>();
        var messageValidator = new Mock<IMessageValidator>();
        var errorLogger = new Mock<IErrorLogger>();
        var capabilityManager = new Mock<ICapabilityManager>();
        var serilog = new Mock<SerilogLogger>();

        var chatServiceFactory = new SalmonEgg.Application.Services.Chat.ChatServiceFactory(
            transportFactory.Object,
            messageParser.Object,
            messageValidator.Object,
            errorLogger.Object,
            capabilityManager.Object,
            sessionManager,
            serilog.Object);

        var configService = new Mock<IConfigurationService>();
        var profilesLogger = new Mock<ILogger<AcpProfilesViewModel>>();
        var profiles = new AcpProfilesViewModel(configService.Object, preferences, profilesLogger.Object);

        var conversationStore = new Mock<IConversationStore>();
        conversationStore.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new ConversationDocument());

        var miniWindow = new Mock<IMiniWindowCoordinator>();
        var vmLogger = new Mock<ILogger<ChatViewModel>>();

        var originalContext = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(syncContext);
        try
        {
            var viewModel = new ChatViewModel(
                chatStore.Object,
                chatServiceFactory,
                configService.Object,
                preferences,
                profiles,
                sessionManager,
                conversationStore.Object,
                miniWindow.Object,
                vmLogger.Object);
            return new ChatViewModelHarness(viewModel, state);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(originalContext);
        }
    }

    private static AppPreferencesViewModel CreatePreferences()
    {
        var appSettingsService = new Mock<IAppSettingsService>();
        appSettingsService.Setup(s => s.LoadAsync()).ReturnsAsync(new AppSettings());
        var startupService = new Mock<IAppStartupService>();
        startupService.SetupGet(s => s.IsSupported).Returns(false);
        var languageService = new Mock<IAppLanguageService>();
        var capabilities = new Mock<IPlatformCapabilityService>();
        var uiRuntime = new Mock<IUiRuntimeService>();
        var prefsLogger = new Mock<ILogger<AppPreferencesViewModel>>();

        return new AppPreferencesViewModel(
            appSettingsService.Object,
            startupService.Object,
            languageService.Object,
            capabilities.Object,
            uiRuntime.Object,
            prefsLogger.Object);
    }

    private sealed class ChatViewModelHarness : IDisposable
    {
        private readonly IState<ChatState> _state;
        public ChatViewModel ViewModel { get; }

        public ChatViewModelHarness(ChatViewModel viewModel, IState<ChatState> state)
        {
            ViewModel = viewModel;
            _state = state;
        }

        public void Dispose()
        {
            ViewModel.Dispose();
            _state.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }
}
