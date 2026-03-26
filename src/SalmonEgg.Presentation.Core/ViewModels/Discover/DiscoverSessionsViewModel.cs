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

public sealed partial class DiscoverSessionsViewModel : ObservableObject
{
    private readonly ILogger<DiscoverSessionsViewModel> _logger;
    private readonly INavigationCoordinator _navigationCoordinator;
    private readonly AcpProfilesViewModel _profilesViewModel;
    private readonly IConnectionService _connectionService;
    private readonly IChatService _chatService;
    private readonly ISessionManager _sessionManager;
    private readonly SynchronizationContext _syncContext;
    private CancellationTokenSource? _loadingCts;

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
            OnPropertyChanged(nameof(SelectedProfile));
            OnPropertyChanged(nameof(HasSelectedProfile));
            OnPropertyChanged(nameof(HasNoSelectedProfile));

            _ = LoadSessionsForProfileAsync(SelectedProfile);
        }
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        await _profilesViewModel.RefreshAsync().ConfigureAwait(false);

        _syncContext.Post(_ =>
        {
            OnPropertyChanged(nameof(AvailableProfiles));
            OnPropertyChanged(nameof(SelectedProfile));
            OnPropertyChanged(nameof(HasSelectedProfile));
            OnPropertyChanged(nameof(HasNoSelectedProfile));

            if (SelectedProfile != null)
            {
                _ = LoadSessionsForProfileAsync(SelectedProfile);
            }
            else if (AvailableProfiles.Any())
            {
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
        _loadingCts?.Cancel();
        _loadingCts = new CancellationTokenSource();
        var token = _loadingCts.Token;

        _syncContext.Post(_ =>
        {
            IsLoading = true;
            ErrorMessage = null;
            AgentSessions.Clear();
        }, null);

        if (profile == null)
        {
            _syncContext.Post(_ => IsLoading = false, null);
            return;
        }

        try
        {
            _syncContext.Post(_ => IsConnecting = true, null);

            var connectionResult = await _connectionService.ConnectAsync(profile.Id).ConfigureAwait(false);
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
                    ErrorMessage = "未找到任何会话。";
                }
            }, null);
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load sessions for profile {ProfileId}", profile.Id);
            _syncContext.Post(_ => ErrorMessage = $"无法同步会话列表: {ex.Message}", null);
        }
        finally
        {
            _syncContext.Post(_ =>
            {
                IsLoading = false;
                IsConnecting = false;
            }, null);
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

            _logger.LogInformation("Loading session {SessionId} from profile {ProfileId}", session.Id, SelectedProfile.Id);

            var success = await _navigationCoordinator.ActivateSessionAsync(session.Id, null).ConfigureAwait(false);

            if (!success)
            {
                _syncContext.Post(_ => ErrorMessage = "加载会话失败，请尝试重新连接。", null);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load session {SessionId}", session.Id);
            _syncContext.Post(_ => ErrorMessage = $"加载会话时出错: {ex.Message}", null);
        }
        finally
        {
            _syncContext.Post(_ => IsLoading = false, null);
        }
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