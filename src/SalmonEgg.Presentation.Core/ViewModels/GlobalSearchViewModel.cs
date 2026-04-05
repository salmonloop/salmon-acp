using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SalmonEgg.Domain.Models;
using SalmonEgg.Domain.Models.Session;
using SalmonEgg.Presentation.Core.Services;
using SalmonEgg.Presentation.Core.Services.Chat;
using SalmonEgg.Presentation.Core.Services.ProjectAffinity;
using SalmonEgg.Presentation.Core.Services.Search;
using SalmonEgg.Presentation.Models.Search;
using SalmonEgg.Presentation.Utilities;
using SalmonEgg.Presentation.ViewModels.Navigation;
using SalmonEgg.Presentation.ViewModels.Settings;

namespace SalmonEgg.Presentation.ViewModels;

public sealed partial class GlobalSearchViewModel : ObservableObject, IDisposable
{
    private const int MaxSearchResults = 50;
    private const int MaxHistoryItems = 10;
    private const int MaxSuggestions = 5;

    private readonly MainNavigationViewModel _navViewModel;
    private readonly AppPreferencesViewModel _preferences;
    private readonly INavigationCoordinator _navigationCoordinator;
    private readonly IConversationCatalogReadModel _conversationCatalog;
    private readonly IProjectAffinityResolver _projectAffinityResolver;
    private readonly IGlobalSearchPipeline _searchPipeline;
    private readonly ILogger<GlobalSearchViewModel> _logger;

    private readonly List<SearchHistoryItem> _searchHistory = new();
    private readonly AsyncQueryCoordinator _searchCoordinator = new();
    private int _activeSearchRequestId;
    private bool _suppressAutoOpen;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasQuery))]
    [NotifyPropertyChangedFor(nameof(IsSearching))]
    [NotifyPropertyChangedFor(nameof(ShowSuggestions))]
    private string _query = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasResults))]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    [NotifyPropertyChangedFor(nameof(HasAnyContent))]
    private ObservableCollection<SearchResultGroup> _resultGroups = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSearching))]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    [NotifyPropertyChangedFor(nameof(IsError))]
    [NotifyPropertyChangedFor(nameof(HasAnyContent))]
    [NotifyPropertyChangedFor(nameof(ShouldShowSearchPanelPresenter))]
    private GlobalSearchViewState _viewState = GlobalSearchViewState.Idle;

    [ObservableProperty]
    private bool _isSearchPanelOpen;

    [ObservableProperty]
    private SearchResultItem? _selectedItem;

    [ObservableProperty]
    private bool _isSearchBoxFocused;

    public bool HasQuery => !string.IsNullOrWhiteSpace(Query);
    public bool IsSearching => ViewState == GlobalSearchViewState.Loading;
    public bool ShowSuggestions => !HasQuery && _searchHistory.Count > 0;
    public bool HasResults => ResultGroups.Count > 0 && ResultGroups.Any(g => g.Items.Count > 0);
    public bool IsEmpty => ViewState == GlobalSearchViewState.Empty;
    public bool IsError => ViewState == GlobalSearchViewState.Error;
    public bool HasAnyContent => HasResults || ShowSuggestions || IsSearching || IsEmpty || IsError;
    public bool ShouldShowSearchPanelPresenter => IsSearchBoxFocused && (HasQuery || ShowSuggestions || IsSearching || HasResults || IsEmpty || IsError);

    public IReadOnlyList<SearchHistoryItem> RecentSearches => _searchHistory.AsReadOnly();

    public GlobalSearchViewModel(
        MainNavigationViewModel navViewModel,
        AppPreferencesViewModel preferences,
        INavigationCoordinator navigationCoordinator,
        IConversationCatalogReadModel conversationCatalog,
        IProjectAffinityResolver projectAffinityResolver,
        IGlobalSearchPipeline searchPipeline,
        ILogger<GlobalSearchViewModel> logger)
    {
        _navViewModel = navViewModel ?? throw new ArgumentNullException(nameof(navViewModel));
        _preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
        _navigationCoordinator = navigationCoordinator ?? throw new ArgumentNullException(nameof(navigationCoordinator));
        _conversationCatalog = conversationCatalog ?? throw new ArgumentNullException(nameof(conversationCatalog));
        _projectAffinityResolver = projectAffinityResolver ?? throw new ArgumentNullException(nameof(projectAffinityResolver));
        _searchPipeline = searchPipeline ?? throw new ArgumentNullException(nameof(searchPipeline));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _resultGroups.CollectionChanged += OnResultGroupsCollectionChanged;
    }

    partial void OnQueryChanged(string value)
    {
        CancelPendingSearch();
        if (!_suppressAutoOpen || !string.IsNullOrWhiteSpace(value))
        {
            _suppressAutoOpen = false;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            ResultGroups.Clear();
            ViewState = GlobalSearchViewState.Idle;
            UpdateSearchPanelState();
            return;
        }

        ResultGroups.Clear();
        ViewState = GlobalSearchViewState.Loading;
        UpdateSearchPanelState();
        var ticket = _searchCoordinator.Begin();
        _ = Interlocked.Increment(ref _activeSearchRequestId);
        _ = SearchAsync(value, ticket);
    }

    private async Task SearchAsync(string query, AsyncQueryCoordinator.QueryTicket ticket)
    {
        var requestId = Volatile.Read(ref _activeSearchRequestId);
        try
        {
            await Task.Delay(150, ticket.Token);
            if (!_searchCoordinator.IsActive(ticket))
            {
                return;
            }

            var sourceSnapshot = BuildSearchSourceSnapshot();
            var result = await Task.Run(
                () => _searchPipeline.SearchAsync(query, sourceSnapshot, ticket.Token),
                ticket.Token).ConfigureAwait(true);

            if (!_searchCoordinator.IsActive(ticket)
                || !string.Equals(Query, query, StringComparison.Ordinal)
                || requestId != Volatile.Read(ref _activeSearchRequestId))
            {
                return;
            }

            ApplySearchSnapshot(result);
            ViewState = ResultGroups.Count > 0
                ? GlobalSearchViewState.Results
                : GlobalSearchViewState.Empty;
            UpdateSearchPanelState();
        }
        catch (OperationCanceledException) when (ticket.Token.IsCancellationRequested)
        {
            // Swallow cancellation; latest request owns the state.
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Search failed for query: {Query}", query);
            if (_searchCoordinator.IsActive(ticket)
                && string.Equals(Query, query, StringComparison.Ordinal)
                && requestId == Volatile.Read(ref _activeSearchRequestId))
            {
                ViewState = GlobalSearchViewState.Error;
                UpdateSearchPanelState();
            }
        }
    }

    private GlobalSearchSourceSnapshot BuildSearchSourceSnapshot()
    {
        var sessions = _conversationCatalog.Snapshot
            .Select(session => new GlobalSearchSessionSource(
                session.ConversationId,
                session.DisplayName ?? SessionNamePolicy.CreateDefault(session.ConversationId),
                session.Cwd))
            .ToImmutableArray();
        var projects = _preferences.Projects
            .Select(project => new GlobalSearchProjectSource(project.ProjectId, project.Name, project.RootPath))
            .ToImmutableArray();
        return new GlobalSearchSourceSnapshot(sessions, projects);
    }

    private void ApplySearchSnapshot(GlobalSearchSnapshot snapshot)
    {
        var topGroups = snapshot.Groups.Take(MaxSearchResults);
        ResultGroups.Clear();
        foreach (var groupSnapshot in topGroups)
        {
            var group = new SearchResultGroup
            {
                Name = groupSnapshot.Name,
                Title = groupSnapshot.Title,
                Priority = groupSnapshot.Priority
            };

            foreach (var item in groupSnapshot.Items)
            {
                group.Items.Add(new SearchResultItem
                {
                    Id = item.Id,
                    Title = item.Title,
                    Subtitle = item.Subtitle,
                    Kind = item.Kind,
                    IconGlyph = item.IconGlyph,
                    Tag = item.Tag,
                    ActivateCommand = SelectResultCommand
                });
            }

            ResultGroups.Add(group);
        }
    }

    [RelayCommand]
    private async Task SelectResultAsync(SearchResultItem item)
    {
        if (item == null)
        {
            return;
        }

        // 添加到历史记录
        AddToHistory(Query);

        // 根据类型处理
        switch (item.Kind)
        {
            case SearchResultKind.Session:
                var session = FindConversation(item.Id);
                await _navigationCoordinator.ActivateSessionAsync(item.Id, GetActivationProjectId(session));
                break;

            case SearchResultKind.Project:
                if (string.Equals(item.Id, MainNavigationViewModel.UnclassifiedProjectId, StringComparison.Ordinal))
                {
                    await _navViewModel.PrepareStartForProjectAsync(MainNavigationViewModel.UnclassifiedProjectId);
                }
                else
                {
                    await _navViewModel.PrepareStartForProjectAsync(item.Id);
                }
                break;

            case SearchResultKind.Setting:
                await _navigationCoordinator.ActivateSettingsAsync(item.Id);
                break;

            case SearchResultKind.Command:
                ExecuteCommand(item);
                break;
        }

        // 关闭搜索面板
        CloseSearchPanel();
    }

    private void ExecuteCommand(SearchResultItem item)
    {
        switch (item.Tag)
        {
            case "new":
                _ = _navViewModel.PrepareStartForProjectAsync(MainNavigationViewModel.UnclassifiedProjectId);
                break;

            case "project":
                _ = _navViewModel.AddProjectCommand.ExecuteAsync(null);
                break;

            case "theme":
                // 切换主题 - 通过偏好设置
                var currentTheme = _preferences.Theme;
                _preferences.Theme = currentTheme switch
                {
                    "Light" => "Dark",
                    "Dark" => "System",
                    _ => "Light"
                };
                break;

            case "animation":
                _preferences.IsAnimationEnabled = !_preferences.IsAnimationEnabled;
                break;
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
        if (conversation == null)
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
            UnclassifiedProjectId: MainNavigationViewModel.UnclassifiedProjectId)).EffectiveProjectId;
    }

    [RelayCommand]
    private void OpenSearchPanel()
    {
        IsSearchPanelOpen = true;
    }

    [RelayCommand]
    private void CloseSearchPanel()
    {
        _suppressAutoOpen = true;
        IsSearchPanelOpen = false;
        Query = string.Empty;
        ResultGroups.Clear();
        ViewState = GlobalSearchViewState.Idle;
    }

    [RelayCommand]
    private void UseHistoryItem(string? query)
    {
        if (!string.IsNullOrWhiteSpace(query))
        {
            Query = query;
        }
    }

    [RelayCommand]
    private void ClearHistory()
    {
        _searchHistory.Clear();
        OnPropertyChanged(nameof(RecentSearches));
        OnPropertyChanged(nameof(ShowSuggestions));
        UpdateSearchPanelState();
    }

    private void AddToHistory(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return;
        }

        // 移除已存在的相同项
        var existingIndex = _searchHistory.FindIndex(item => string.Equals(item.Query, query, StringComparison.Ordinal));
        if (existingIndex >= 0)
        {
            _searchHistory.RemoveAt(existingIndex);
        }

        // 添加到开头
        _searchHistory.Insert(0, new SearchHistoryItem
        {
            Query = query,
            UseCommand = UseHistoryItemCommand
        });

        // 限制历史记录数量
        while (_searchHistory.Count > MaxHistoryItems)
        {
            _searchHistory.RemoveAt(_searchHistory.Count - 1);
        }

        OnPropertyChanged(nameof(RecentSearches));
        OnPropertyChanged(nameof(ShowSuggestions));
        UpdateSearchPanelState();
    }

    partial void OnIsSearchBoxFocusedChanged(bool value)
    {
        if (value)
        {
            _suppressAutoOpen = false;
        }

        UpdateSearchPanelState();
    }

    private void UpdateSearchPanelState()
    {
        if (!IsSearchBoxFocused || !ShouldShowSearchPanelPresenter)
        {
            IsSearchPanelOpen = false;
            return;
        }

        if (_suppressAutoOpen)
        {
            return;
        }

        IsSearchPanelOpen = true;
    }

    public void FocusSearch()
    {
        IsSearchPanelOpen = true;
    }

    private void CancelPendingSearch()
    {
        _searchCoordinator.Cancel();
    }

    private void OnResultGroupsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasResults));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(IsError));
        OnPropertyChanged(nameof(HasAnyContent));
        OnPropertyChanged(nameof(ShouldShowSearchPanelPresenter));
    }

    partial void OnResultGroupsChanged(ObservableCollection<SearchResultGroup>? oldValue, ObservableCollection<SearchResultGroup> newValue)
    {
        if (oldValue != null)
        {
            oldValue.CollectionChanged -= OnResultGroupsCollectionChanged;
        }

        newValue.CollectionChanged += OnResultGroupsCollectionChanged;
    }

    public void Dispose()
    {
        ResultGroups.CollectionChanged -= OnResultGroupsCollectionChanged;
        _searchCoordinator.Dispose();
    }
}
