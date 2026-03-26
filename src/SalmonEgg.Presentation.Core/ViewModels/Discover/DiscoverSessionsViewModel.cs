using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isConnecting;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    [ObservableProperty]
    private ServerConfiguration? _selectedProfile;

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
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        await _profilesViewModel.RefreshAsync();

        if (AvailableProfiles.Any())
        {
            // Default to the last used profile or the first one
            SelectedProfile = AvailableProfiles.FirstOrDefault(p => p.Id == _profilesViewModel.SelectedProfile?.Id)
                              ?? AvailableProfiles.FirstOrDefault();
        }
    }

    partial void OnSelectedProfileChanged(ServerConfiguration? value)
    {
        if (value != null)
        {
            _ = LoadSessionsForProfileAsync(value);
        }
        else
        {
            AgentSessions.Clear();
        }
    }

    [RelayCommand]
    private async Task RefreshSessionsAsync()
    {
        if (SelectedProfile != null)
        {
            await LoadSessionsForProfileAsync(SelectedProfile);
        }
    }

    private async Task LoadSessionsForProfileAsync(ServerConfiguration profile)
    {
        IsLoading = true;
        ErrorMessage = null;
        AgentSessions.Clear();

        try
        {
            // Ensure we are connected to the selected profile
            IsConnecting = true;

            await _connectionService.ConnectAsync(profile.Id);

            IsConnecting = false;

            // Initialize chat if needed
            if (!_chatService.IsInitialized)
            {
                await _chatService.InitializeAsync(new InitializeParams
                {
                    ProtocolVersion = 1,
                    ClientInfo = new ClientInfo { Name = "SalmonEgg", Version = "1.0.0" },
                    ClientCapabilities = new ClientCapabilities()
                });
            }

            // Request the session list
            var listResponse = await _chatService.ListSessionsAsync(new SessionListParams());

            if (listResponse?.Sessions != null)
            {
                foreach (var session in listResponse.Sessions)
                {
                    DateTime lastModified = DateTime.Now;
                    if (!string.IsNullOrEmpty(session.LastModified) && DateTime.TryParse(session.LastModified, out var parsed))
                    {
                        lastModified = parsed;
                    }

                    AgentSessions.Add(new DiscoverSessionItemViewModel(
                        session.SessionId,
                        string.IsNullOrWhiteSpace(session.Title) ? "未命名会话" : session.Title,
                        string.IsNullOrWhiteSpace(session.Description) ? "暂无描述" : session.Description,
                        lastModified));
                }
            }

            if (AgentSessions.Count == 0)
            {
                ErrorMessage = "未找到任何会话。";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load sessions for profile {ProfileId}", profile.Id);
            ErrorMessage = $"无法加载会话列表: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            IsConnecting = false;
        }
    }

    [RelayCommand]
    private async Task LoadSessionAsync(DiscoverSessionItemViewModel? session)
    {
        if (session == null || SelectedProfile == null) return;

        try
        {
            IsLoading = true;
            ErrorMessage = null;

            _logger.LogInformation("Loading session {SessionId} from profile {ProfileId}", session.Id, SelectedProfile.Id);

            // Import/create local session mapping if necessary
            // NavigationCoordinator will handle the routing and activation
            var success = await _navigationCoordinator.ActivateSessionAsync(session.Id, null);

            if (!success)
            {
                ErrorMessage = "加载会话失败，请重试。";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load session {SessionId}", session.Id);
            ErrorMessage = $"加载会话时出错: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
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