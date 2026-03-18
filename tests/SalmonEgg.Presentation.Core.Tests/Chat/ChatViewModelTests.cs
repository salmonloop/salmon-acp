using System;
using System.Collections;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using SalmonEgg.Application.Services.Chat;
using SalmonEgg.Domain.Interfaces;
using SalmonEgg.Domain.Interfaces.Transport;
using SalmonEgg.Domain.Models;
using SalmonEgg.Domain.Services;
using SalmonEgg.Presentation.Services;
using SalmonEgg.Presentation.Core.Mvux.Chat;
using SalmonEgg.Presentation.ViewModels.Chat;
using SalmonEgg.Presentation.ViewModels.Settings;
using SerilogLogger = Serilog.ILogger;
using Uno.Extensions.Reactive;
using Xunit;

namespace SalmonEgg.Presentation.Core.Tests.Chat;

[Collection("NonParallel")]
public class ChatViewModelTests
{
    private static ViewModelFixture CreateViewModel(SynchronizationContext? syncContext = null)
    {
        var state = State.Value(new object(), () => ChatState.Empty);
        var chatStore = new Mock<IChatStore>();
        chatStore.Setup(s => s.State).Returns(state);
        chatStore.Setup(s => s.Dispatch(It.IsAny<ChatAction>()))
            .Returns<ChatAction>(action => state.Update(s => ChatReducer.Reduce(s!, action), default));
        var transportFactory = new Mock<ITransportFactory>();
        var messageParser = new Mock<IMessageParser>();
        var messageValidator = new Mock<IMessageValidator>();
        var errorLogger = new Mock<IErrorLogger>();
        var capabilityManager = new Mock<ICapabilityManager>();
        var sessionManager = new Mock<ISessionManager>();
        var serilog = new Mock<SerilogLogger>();

        var chatServiceFactory = new ChatServiceFactory(
            transportFactory.Object,
            messageParser.Object,
            messageValidator.Object,
            errorLogger.Object,
            capabilityManager.Object,
            sessionManager.Object,
            serilog.Object);

        var configService = new Mock<IConfigurationService>();
        var appSettingsService = new Mock<IAppSettingsService>();
        appSettingsService.Setup(s => s.LoadAsync()).ReturnsAsync(new AppSettings());
        var startupService = new Mock<IAppStartupService>();
        startupService.SetupGet(s => s.IsSupported).Returns(false);
        var languageService = new Mock<IAppLanguageService>();
        var capabilities = new Mock<IPlatformCapabilityService>();
        var uiRuntime = new Mock<IUiRuntimeService>();
        var prefsLogger = new Mock<ILogger<AppPreferencesViewModel>>();

        var preferences = new AppPreferencesViewModel(
            appSettingsService.Object,
            startupService.Object,
            languageService.Object,
            capabilities.Object,
            uiRuntime.Object,
            prefsLogger.Object);

        var profilesLogger = new Mock<ILogger<AcpProfilesViewModel>>();
        var profiles = new AcpProfilesViewModel(configService.Object, preferences, profilesLogger.Object);

        var conversationStore = new Mock<IConversationStore>();
        var miniWindow = new Mock<IMiniWindowCoordinator>();
        var vmLogger = new Mock<ILogger<ChatViewModel>>();

        var originalContext = SynchronizationContext.Current;
        try
        {
            SynchronizationContext.SetSynchronizationContext(syncContext ?? new SynchronizationContext());

            var viewModel = new ChatViewModel(
                chatStore.Object,
                chatServiceFactory,
                configService.Object,
                preferences,
                profiles,
                sessionManager.Object,
                conversationStore.Object,
                miniWindow.Object,
                vmLogger.Object,
                syncContext);
            return new ViewModelFixture(viewModel, state, chatStore.Object);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(originalContext);
        }
    }

    [Fact]
    public async System.Threading.Tasks.Task TrySwitchToSessionAsync_NewSession_DoesNotSeedRemoteSessionId()
    {
        using var fixture = CreateViewModel();
        var viewModel = fixture.ViewModel;
        var localSessionId = Guid.NewGuid().ToString("N");

        await viewModel.TrySwitchToSessionAsync(localSessionId);

        var field = typeof(ChatViewModel).GetField("_conversationBindings", BindingFlags.Instance | BindingFlags.NonPublic);
        var bindings = (IDictionary)field!.GetValue(viewModel)!;
        var binding = bindings[localSessionId];
        var remoteProp = binding?.GetType().GetProperty("RemoteSessionId", BindingFlags.Instance | BindingFlags.Public);
        var remote = (string?)remoteProp?.GetValue(binding);

        Assert.Null(remote);
    }

    [Fact]
    public async Task TrySwitchToSessionAsync_WaitsForUiStateBeforeCompleting()
    {
        var syncContext = new QueueingSynchronizationContext();
        using var fixture = CreateViewModel(syncContext);
        var viewModel = fixture.ViewModel;
        var sessionId = Guid.NewGuid().ToString("N");
        var syncField = typeof(ChatViewModel).GetField("_syncContext", BindingFlags.Instance | BindingFlags.NonPublic);
        var capturedContext = syncField?.GetValue(viewModel);
        Assert.Same(syncContext, capturedContext);
        var gateField = typeof(ChatViewModel).GetField("_sessionSwitchGate", BindingFlags.Instance | BindingFlags.NonPublic);
        var gate = (SemaphoreSlim?)gateField?.GetValue(viewModel);
        Assert.NotNull(gate);
        Assert.Equal(1, gate!.CurrentCount);

        var switchTask = viewModel.TrySwitchToSessionAsync(sessionId);
        await Task.Yield();
        Assert.False(switchTask.IsCompleted);
        for (var i = 0; i < 4 && !switchTask.IsCompleted; i++)
        {
            syncContext.RunAll();
            await Task.Delay(TimeSpan.FromMilliseconds(10));
        }

        var completed = await Task.WhenAny(switchTask, Task.Delay(TimeSpan.FromSeconds(2)));
        Assert.Same(switchTask, completed);
        await switchTask;

        Assert.Equal(sessionId, viewModel.CurrentSessionId);
    }

    [Fact]
    public async Task Dispose_CancelsStoreSubscription_DoesNotUpdateAfterDispose()
    {
        // 1. Setup store with initial state
        var initialState = ChatState.Empty with { IsThinking = false };
        var chatStore = new Mock<IChatStore>();
        await using var state = State.Value(this, () => initialState);
        chatStore.Setup(s => s.State).Returns(state);
        chatStore.Setup(s => s.Dispatch(It.IsAny<ChatAction>()))
            .Returns<ChatAction>(action => state.Update(s => ChatReducer.Reduce(s!, action), CancellationToken.None));

        // 2. Create VM with queueing sync context
        var syncContext = new QueueingSynchronizationContext();
        var transportFactory = new Mock<ITransportFactory>();
        var messageParser = new Mock<IMessageParser>();
        var messageValidator = new Mock<IMessageValidator>();
        var errorLogger = new Mock<IErrorLogger>();
        var capabilityManager = new Mock<ICapabilityManager>();
        var sessionManager = new Mock<ISessionManager>();
        var serilog = new Mock<Serilog.ILogger>();

        var chatServiceFactory = new ChatServiceFactory(
            transportFactory.Object,
            messageParser.Object,
            messageValidator.Object,
            errorLogger.Object,
            capabilityManager.Object,
            sessionManager.Object,
            serilog.Object);

        var configService = new Mock<IConfigurationService>();
        var appSettingsService = new Mock<IAppSettingsService>();
        appSettingsService.Setup(s => s.LoadAsync()).ReturnsAsync(new AppSettings());
        var startupService = new Mock<IAppStartupService>();
        var languageService = new Mock<IAppLanguageService>();
        var capabilities = new Mock<IPlatformCapabilityService>();
        var uiRuntime = new Mock<IUiRuntimeService>();
        var prefsLogger = new Mock<ILogger<AppPreferencesViewModel>>();

        var preferences = new AppPreferencesViewModel(
            appSettingsService.Object,
            startupService.Object,
            languageService.Object,
            capabilities.Object,
            uiRuntime.Object,
            prefsLogger.Object);

        var profilesLogger = new Mock<ILogger<AcpProfilesViewModel>>();
        var profiles = new AcpProfilesViewModel(configService.Object, preferences, profilesLogger.Object);
        var conversationStore = new Mock<IConversationStore>();
        var miniWindow = new Mock<IMiniWindowCoordinator>();
        var vmLogger = new Mock<ILogger<ChatViewModel>>();

        using var viewModel = new ChatViewModel(
            chatStore.Object,
            chatServiceFactory,
            configService.Object,
            preferences,
            profiles,
            sessionManager.Object,
            conversationStore.Object,
            miniWindow.Object,
            vmLogger.Object,
            syncContext);

        // 3. Dispatch initial state update and verify projection
        syncContext.RunAll();
        Assert.False(viewModel.IsThinking);

        // 4. Dispose the ViewModel
        viewModel.Dispose();

        // 5. Update store state (thinking = true)
        // Note: In real MVUX, the ForEachAsync loop will stop due to CTS cancellation.
        // For this test to be robust, we verify that after dispose, no further updates reach the UI properties.
        var newState = initialState with { IsThinking = true };
        await state.Update(s => newState, CancellationToken.None);

        // 6. Flush sync context and verify thinking is still false
        syncContext.RunAll();
        Assert.False(viewModel.IsThinking);
    }

    [Fact]
    public async Task CurrentPrompt_UpdatesDraftTextInStore()
    {
        using var fixture = CreateViewModel();
        var viewModel = fixture.ViewModel;

        viewModel.CurrentPrompt = "draft text";

        await Task.Delay(50);

        Assert.Equal("draft text", viewModel.CurrentPrompt);
        Assert.Equal("draft text", (await fixture.GetStateAsync()).DraftText);
    }

    [Fact]
    public async Task StoreDraftText_ProjectsToCurrentPrompt()
    {
        using var fixture = CreateViewModel();
        var viewModel = fixture.ViewModel;

        await fixture.DispatchAsync(new SetDraftTextAction("from store"));
        await Task.Delay(50);

        Assert.Equal("from store", viewModel.CurrentPrompt);
    }

    private sealed class QueueingSynchronizationContext : SynchronizationContext
    {
        private readonly Queue<(SendOrPostCallback callback, object? state)> _work = new();

        public override void Post(SendOrPostCallback d, object? state)
        {
            _work.Enqueue((d, state));
        }

        public void RunAll()
        {
            while (_work.Count > 0)
            {
                var (callback, state) = _work.Dequeue();
                callback(state);
            }
        }
    }

    private sealed class ViewModelFixture : IDisposable
    {
        private readonly IState<ChatState> _state;
        private readonly IChatStore _store;
        public ChatViewModel ViewModel { get; }

        public ViewModelFixture(ChatViewModel viewModel, IState<ChatState> state, IChatStore store)
        {
            ViewModel = viewModel;
            _state = state;
            _store = store;
        }

        public async Task<ChatState> GetStateAsync() => await _state ?? ChatState.Empty;

        public ValueTask DispatchAsync(ChatAction action) => _store.Dispatch(action);

        public void Dispose()
        {
            ViewModel.Dispose();
            _state.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }
}
