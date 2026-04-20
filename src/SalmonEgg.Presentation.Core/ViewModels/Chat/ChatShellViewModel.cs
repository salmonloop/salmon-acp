using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using SalmonEgg.Presentation.Core.Services;
using SalmonEgg.Presentation.Core.Services.Chat;
using SalmonEgg.Presentation.Core.Services.ProjectAffinity;
using SalmonEgg.Presentation.Core.ViewModels.ShellLayout;
using SalmonEgg.Presentation.ViewModels.Settings;

namespace SalmonEgg.Presentation.ViewModels.Chat;

public sealed partial class ChatShellViewModel : ObservableObject
{
    private readonly INavigationCoordinator _navigationCoordinator;
    private readonly IConversationCatalogReadModel _conversationCatalog;
    private readonly IProjectAffinityResolver _projectAffinityResolver;
    private readonly AppPreferencesViewModel _preferences;
    private readonly ILogger<ChatShellViewModel> _logger;
    private bool _suppressMiniWindowSelectionSync;

    public ChatShellViewModel(
        ChatViewModel chat,
        ShellLayoutViewModel shellLayout,
        INavigationCoordinator navigationCoordinator,
        IConversationCatalogReadModel conversationCatalog,
        IProjectAffinityResolver projectAffinityResolver,
        AppPreferencesViewModel preferences,
        ILogger<ChatShellViewModel> logger)
    {
        Chat = chat ?? throw new ArgumentNullException(nameof(chat));
        ShellLayout = shellLayout ?? throw new ArgumentNullException(nameof(shellLayout));
        _navigationCoordinator = navigationCoordinator ?? throw new ArgumentNullException(nameof(navigationCoordinator));
        _conversationCatalog = conversationCatalog ?? throw new ArgumentNullException(nameof(conversationCatalog));
        _projectAffinityResolver = projectAffinityResolver ?? throw new ArgumentNullException(nameof(projectAffinityResolver));
        _preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Chat.PropertyChanged += OnChatPropertyChanged;
        Chat.MiniWindowSessions.CollectionChanged += OnMiniWindowSessionsCollectionChanged;
        SyncSelectedMiniWindowSession();
    }

    public ChatViewModel Chat { get; }

    public ShellLayoutViewModel ShellLayout { get; }

    public ObservableCollection<MiniWindowConversationItemViewModel> MiniWindowSessions => Chat.MiniWindowSessions;

    [ObservableProperty]
    private MiniWindowConversationItemViewModel? _selectedMiniWindowSession;

    partial void OnSelectedMiniWindowSessionChanged(MiniWindowConversationItemViewModel? value)
    {
        if (_suppressMiniWindowSelectionSync
            || value is null
            || string.IsNullOrWhiteSpace(value.ConversationId)
            || string.Equals(Chat.CurrentSessionId, value.ConversationId, StringComparison.Ordinal))
        {
            return;
        }

        ActivateMiniWindowSessionAsync(value.ConversationId);
    }

    private async void ActivateMiniWindowSessionAsync(string conversationId)
    {
        try
        {
            await _navigationCoordinator
                .ActivateSessionAsync(conversationId, GetActivationProjectId(FindConversation(conversationId)))
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to activate mini window conversation {ConversationId}", conversationId);
        }
    }

    private void OnChatPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.Equals(e.PropertyName, nameof(ChatViewModel.CurrentSessionId), StringComparison.Ordinal)
            || string.Equals(e.PropertyName, nameof(ChatViewModel.MiniWindowSessions), StringComparison.Ordinal))
        {
            SyncSelectedMiniWindowSession();
        }

        if (string.Equals(e.PropertyName, nameof(ChatViewModel.MiniWindowSessions), StringComparison.Ordinal))
        {
            RewireMiniWindowSessionsCollection();
        }
    }

    private void RewireMiniWindowSessionsCollection()
    {
        Chat.MiniWindowSessions.CollectionChanged -= OnMiniWindowSessionsCollectionChanged;
        Chat.MiniWindowSessions.CollectionChanged += OnMiniWindowSessionsCollectionChanged;
        OnPropertyChanged(nameof(MiniWindowSessions));
        SyncSelectedMiniWindowSession();
    }

    private void OnMiniWindowSessionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SyncSelectedMiniWindowSession();
    }

    private void SyncSelectedMiniWindowSession()
    {
        if (_suppressMiniWindowSelectionSync)
        {
            return;
        }

        try
        {
            _suppressMiniWindowSelectionSync = true;
            if (string.IsNullOrWhiteSpace(Chat.CurrentSessionId))
            {
                SelectedMiniWindowSession = null;
                return;
            }

            var match = MiniWindowSessions.FirstOrDefault(item =>
                string.Equals(item.ConversationId, Chat.CurrentSessionId, StringComparison.Ordinal));
            if (!ReferenceEquals(SelectedMiniWindowSession, match))
            {
                SelectedMiniWindowSession = match;
            }
        }
        finally
        {
            _suppressMiniWindowSelectionSync = false;
        }
    }

    private ConversationCatalogItem? FindConversation(string conversationId)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
        {
            return null;
        }

        return _conversationCatalog.Snapshot
            .FirstOrDefault(item => string.Equals(item.ConversationId, conversationId, StringComparison.Ordinal));
    }

    private string? GetActivationProjectId(ConversationCatalogItem? conversation)
    {
        if (conversation is null)
        {
            return null;
        }

        return _projectAffinityResolver.Resolve(new ProjectAffinityRequest(
            RemoteCwd: conversation.Cwd,
            BoundProfileId: conversation.BoundProfileId,
            RemoteSessionId: conversation.RemoteSessionId,
            OverrideProjectId: conversation.ProjectAffinityOverrideProjectId,
            Projects: _preferences.Projects,
            PathMappings: _preferences.ProjectPathMappings,
            UnclassifiedProjectId: NavigationProjectIds.Unclassified)).EffectiveProjectId;
    }
}
