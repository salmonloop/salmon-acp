using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SalmonEgg.Application.Services;
using SalmonEgg.Application.Services.Chat;
using SalmonEgg.Domain.Models;
using SalmonEgg.Domain.Models.Protocol;
using SalmonEgg.Domain.Services;
using SalmonEgg.Presentation.Core.Services;
using SalmonEgg.Presentation.ViewModels.Settings;

namespace SalmonEgg.Presentation.ViewModels.Discover;

public sealed partial class DiscoverSessionsViewModel : ObservableObject, IDisposable
{
    private readonly ILogger<DiscoverSessionsViewModel> _logger;
    private readonly INavigationCoordinator _navigationCoordinator;
    private readonly AcpProfilesViewModel _profilesViewModel;
    private readonly IConnectionService _connectionService;
    private readonly IChatService _chatService;
    private readonly ISessionManager _sessionManager;
    private readonly SynchronizationContext _syncContext;
    private CancellationTokenSource? _loadingCts;
    private bool _disposed;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isConnecting;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public AcpProfilesViewModel ProfilesViewModel => _profilesViewModel;

    public ServerConfiguration? SelectedProfile
    {
        get => _profilesViewModel.SelectedProfile;
        set => _profilesViewModel.SelectedProfile = value;
    }

    public bool HasSelectedProfile => SelectedProfile != null;

    public bool HasNoSelectedProfile => SelectedProfile == null;

    public ObservableCollection<ServerConfiguration> AvailableProfiles => _profilesViewModel.Profiles;

    public ObservableCollection<DiscoverSessionItemViewModel> AgentSessions { get; } = new();

    public DiscoverSessionsViewModel(
        ILogger<DiscoverSessionsViewModel> logger,
        INavigationCoordinator navigationCoordinator,
        AcpProfilesViewModel profilesViewModel,
        IConnectionService connectionService,
        IChatService chatService,
        ISessionManager sessionManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _navigationCoordinator = navigationCoordinator ?? throw new ArgumentNullException(nameof(navigationCoordinator));
        _profilesViewModel = profilesViewModel ?? throw new ArgumentNullException(nameof(profilesViewModel));
        _connectionService = connectionService ?? throw new ArgumentNullException(nameof(connectionService));
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
        _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        _syncContext = SynchronizationContext.Current ?? new SynchronizationContext();

        _profilesViewModel.PropertyChanged += OnProfilesViewModelPropertyChanged;
    }

    private void OnProfilesViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AcpProfilesViewModel.SelectedProfile))
        {
            _syncContext.Post(_ =>
            {
                OnPropertyChanged(nameof(SelectedProfile));
                OnPropertyChanged(nameof(HasSelectedProfile));
                OnPropertyChanged(nameof(HasNoSelectedProfile));

                _ = LoadSessionsForProfileAsync(SelectedProfile);
            }, null);
        }
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        // SSOT: Only refresh if list is empty to avoid redundant network calls if already loaded in Settings
        await _profilesViewModel.RefreshIfEmptyAsync().ConfigureAwait(false);

        _syncContext.Post(_ =>
        {
            OnPropertyChanged(nameof(AvailableProfiles));
            OnPropertyChanged(nameof(SelectedProfile));
            OnPropertyChanged(nameof(HasSelectedProfile));
            OnPropertyChanged(nameof(HasNoSelectedProfile));

            if (SelectedProfile != null)
            {
                // If we already have a selection, load it if the session list is empty
                if (AgentSessions.Count == 0 && !IsLoading)
                {
                    _ = LoadSessionsForProfileAsync(SelectedProfile);
                }
            }
            else if (AvailableProfiles.Any())
            {
                // Default to first profile if nothing selected
                SelectedProfile = AvailableProfiles.First();
            }
        }, null);
    }

    [RelayCommand]
    private async Task RefreshSessionsAsync()
    {
        if (SelectedProfile != null)
        {
            await LoadSessionsForProfileAsync(SelectedProfile).ConfigureAwait(false);
        }
    }

    private async Task LoadSessionsForProfileAsync(ServerConfiguration? profile)
    {
        if (profile == null)
        {
            _syncContext.Post(_ => {
                AgentSessions.Clear();
                IsLoading = false;
                IsConnecting = false;
            }, null);
            return;
        }

        // Cancel any pending load
        _loadingCts?.Cancel();
        _loadingCts = new CancellationTokenSource();
        var token = _loadingCts.Token;

        _syncContext.Post(_ =>
        {
            IsLoading = true;
            IsConnecting = true;
            ErrorMessage = null;
            AgentSessions.Clear();
        }, null);

        try
        {
            // Connect to the agent
            var connectionResult = await _connectionService.ConnectAsync(profile.Id).ConfigureAwait(false);
            if (token.IsCancellationRequested) return;

            if (!connectionResult.IsSuccess)
            {
                _syncContext.Post(_ =>
                {
                    ErrorMessage = $"连接 Agent 失败: {connectionResult.Error}";
                    IsLoading = false;
                    IsConnecting = false;
                }, null);
                return;
            }

            _syncContext.Post(_ => IsConnecting = false, null);

            // Initialize chat service if needed
            if (!_chatService.IsInitialized)
            {
                await _chatService.InitializeAsync(new InitializeParams
                {
                    ProtocolVersion = 1,
                    ClientInfo = new ClientInfo { Name = "SalmonEgg", Version = "1.0.0" },
                    ClientCapabilities = new ClientCapabilities()
                }).ConfigureAwait(false);
            }

            if (token.IsCancellationRequested) return;

            // List sessions
            var listResponse = await _chatService.ListSessionsAsync(new SessionListParams(), token).ConfigureAwait(false);

            if (token.IsCancellationRequested) return;

            var items = new List<DiscoverSessionItemViewModel>();
            if (listResponse?.Sessions != null)
            {
                foreach (var session in listResponse.Sessions)
                {
                    DateTime lastModified = DateTime.Now;
                    if (!string.IsNullOrEmpty(session.UpdatedAt) && DateTime.TryParse(session.UpdatedAt, out var parsed))
                    {
                        lastModified = parsed;
                    }

                    items.Add(new DiscoverSessionItemViewModel(
                        session.SessionId,
                        string.IsNullOrWhiteSpace(session.Title) ? "未命名会话" : session.Title,
                        string.IsNullOrWhiteSpace(session.Description) ? "暂无描述" : session.Description,
                        lastModified));
                }
            }

            _syncContext.Post(_ =>
            {
                foreach (var item in items)
                {
                    AgentSessions.Add(item);
                }

                if (AgentSessions.Count == 0 && !HasError)
                {
                    ErrorMessage = "未找到任何可用会话。";
                }
            }, null);
        }
        catch (OperationCanceledException)
        {
            // Ignore
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load sessions for profile {ProfileId}", profile.Id);
            _syncContext.Post(_ => ErrorMessage = $"无法获取会话列表: {ex.Message}", null);
        }
        finally
        {
            if (!token.IsCancellationRequested)
            {
                _syncContext.Post(_ =>
                {
                    IsLoading = false;
                    IsConnecting = false;
                }, null);
            }
        }
    }

    [RelayCommand]
    private async Task LoadSessionAsync(DiscoverSessionItemViewModel? session)
    {
        if (session == null || SelectedProfile == null) return;

        try
        {
            _syncContext.Post(_ =>
            {
                IsLoading = true;
                ErrorMessage = null;
            }, null);

            _logger.LogInformation("Importing session {SessionId} from profile {ProfileId}", session.Id, SelectedProfile.Id);

            var success = await _navigationCoordinator.ActivateSessionAsync(session.Id, null).ConfigureAwait(false);

            if (!success)
            {
                _syncContext.Post(_ => ErrorMessage = "加载会话并导入失败，请检查连接状态。", null);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load session {SessionId}", session.Id);
            _syncContext.Post(_ => ErrorMessage = $"导入会话时出错: {ex.Message}", null);
        }
        finally
        {
            _syncContext.Post(_ => IsLoading = false, null);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        _profilesViewModel.PropertyChanged -= OnProfilesViewModelPropertyChanged;
        _loadingCts?.Cancel();
        _loadingCts?.Dispose();
    }
}

public sealed class DiscoverSessionItemViewModel
{
    public string Id { get; }
    public string Title { get; }
    public string Description { get; }
    public DateTime LastModified { get; }
    public string FormattedDate => LastModified.ToString("yyyy-MM-dd HH:mm");

    public DiscoverSessionItemViewModel(string id, string title, string description, DateTime lastModified)
    {
        Id = id;
        Title = title;
        Description = description;
        LastModified = lastModified;
    }
}