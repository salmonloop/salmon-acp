using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using SalmonEgg.Domain.Models;
using SalmonEgg.Presentation.Core.Services.Chat;
using SalmonEgg.Presentation.ViewModels.Chat;

namespace SalmonEgg.Presentation.ViewModels.Settings;

public sealed partial class AcpConnectionSettingsViewModel : ObservableObject, IDisposable
{
    private readonly ILogger<AcpConnectionSettingsViewModel> _logger;
    private readonly AppPreferencesViewModel _preferences;
    private readonly ISettingsAcpConnectionState _connectionState;
    private readonly ISettingsAcpConnectionCommands _connectionCommands;
    private readonly ISettingsAcpTransportConfiguration _transportConfig;
    private bool _disposed;

    public ISettingsChatConnection Chat { get; }
    public AcpProfilesViewModel Profiles { get; }

    public ObservableCollection<TransportOptionViewModel> TransportOptions { get; } = new()
    {
        new TransportOptionViewModel(TransportType.Stdio, "Stdio（本地）"),
        new TransportOptionViewModel(TransportType.WebSocket, "WebSocket"),
        new TransportOptionViewModel(TransportType.HttpSse, "HTTP SSE"),
    };

    [ObservableProperty]
    private TransportOptionViewModel? _selectedTransport;

    public string SelectedTransportName => SelectedTransport?.Name ?? string.Empty;

    public string AgentDisplayName =>
        ResolveConnectedProfileName()
        ?? (string.IsNullOrWhiteSpace(_connectionState.AgentName) ? "Agent" : _connectionState.AgentName!);

    public string ConnectionStatusText
    {
        get
        {
            if (_connectionState.IsConnecting || _connectionState.IsInitializing)
            {
                return "正在连接…";
            }

            if (_connectionState.IsConnected)
            {
                return "已连接";
            }

            if (_connectionState.HasConnectionError)
            {
                return "连接失败";
            }

            return "未连接";
        }
    }

    public string CurrentEndpointDisplay
    {
        get
        {
            if (_transportConfig.SelectedTransportType == TransportType.Stdio)
            {
                var cmd = (_transportConfig.StdioCommand ?? string.Empty).Trim();
                var args = (_transportConfig.StdioArgs ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(cmd))
                {
                    return "—";
                }

                return string.IsNullOrWhiteSpace(args) ? cmd : $"{cmd} {args}";
            }

            var url = (_transportConfig.RemoteUrl ?? string.Empty).Trim();
            return string.IsNullOrWhiteSpace(url) ? "—" : url;
        }
    }

    public AcpConnectionSettingsViewModel(
        ChatViewModel chatViewModel,
        AcpProfilesViewModel profiles,
        AppPreferencesViewModel preferences,
        ILogger<AcpConnectionSettingsViewModel> logger)
        : this(new SettingsChatConnectionAdapter(chatViewModel), profiles, preferences, logger)
    {
    }

    public AcpConnectionSettingsViewModel(
        ISettingsChatConnection chat,
        AcpProfilesViewModel profiles,
        AppPreferencesViewModel preferences,
        ILogger<AcpConnectionSettingsViewModel> logger)
        : this(chat, chat, chat.TransportConfig, profiles, preferences, logger, chat)
    {
    }

    public AcpConnectionSettingsViewModel(
        ISettingsAcpConnectionState connectionState,
        ISettingsAcpConnectionCommands connectionCommands,
        ISettingsAcpTransportConfiguration transportConfig,
        AcpProfilesViewModel profiles,
        AppPreferencesViewModel preferences,
        ILogger<AcpConnectionSettingsViewModel> logger)
        : this(connectionState, connectionCommands, transportConfig, profiles, preferences, logger, null)
    {
    }

    private AcpConnectionSettingsViewModel(
        ISettingsAcpConnectionState connectionState,
        ISettingsAcpConnectionCommands connectionCommands,
        ISettingsAcpTransportConfiguration transportConfig,
        AcpProfilesViewModel profiles,
        AppPreferencesViewModel preferences,
        ILogger<AcpConnectionSettingsViewModel> logger,
        ISettingsChatConnection? chatFacade)
    {
        _connectionState = connectionState ?? throw new ArgumentNullException(nameof(connectionState));
        _connectionCommands = connectionCommands ?? throw new ArgumentNullException(nameof(connectionCommands));
        _transportConfig = transportConfig ?? throw new ArgumentNullException(nameof(transportConfig));
        Chat = chatFacade ?? new CompositeSettingsChatConnection(_connectionState, _connectionCommands, _transportConfig);
        Profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
        _preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        SelectedTransport = TransportOptions.FirstOrDefault(o => o.Type == _transportConfig.SelectedTransportType)
                            ?? TransportOptions.First();

        _transportConfig.PropertyChanged += OnTransportConfigPropertyChanged;
        _connectionState.PropertyChanged += OnChatPropertyChanged;
        Profiles.Profiles.CollectionChanged += OnProfilesCollectionChanged;
        _preferences.PropertyChanged += OnPreferencesPropertyChanged;
    }

    private void OnProfilesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(AgentDisplayName));
    }

    private void OnPreferencesPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppPreferencesViewModel.LastSelectedServerId))
        {
            OnPropertyChanged(nameof(AgentDisplayName));
        }
    }

    private void OnChatPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ISettingsAcpConnectionState.AgentName))
        {
            OnPropertyChanged(nameof(AgentDisplayName));
        }

        if (e.PropertyName == nameof(ISettingsAcpConnectionState.IsConnected) ||
            e.PropertyName == nameof(ISettingsAcpConnectionState.IsConnecting) ||
            e.PropertyName == nameof(ISettingsAcpConnectionState.IsInitializing) ||
            e.PropertyName == nameof(ISettingsAcpConnectionState.ConnectionErrorMessage) ||
            e.PropertyName == nameof(ISettingsAcpConnectionState.HasConnectionError))
        {
            OnPropertyChanged(nameof(ConnectionStatusText));
        }
    }

    private void OnTransportConfigPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ISettingsAcpTransportConfiguration.SelectedTransportType))
        {
            var current = TransportOptions.FirstOrDefault(o => o.Type == _transportConfig.SelectedTransportType);
            if (current != null && SelectedTransport?.Type != current.Type)
            {
                SelectedTransport = current;
            }
        }

        if (e.PropertyName == nameof(ISettingsAcpTransportConfiguration.SelectedTransportType) ||
            e.PropertyName == nameof(ISettingsAcpTransportConfiguration.RemoteUrl) ||
            e.PropertyName == nameof(ISettingsAcpTransportConfiguration.StdioCommand) ||
            e.PropertyName == nameof(ISettingsAcpTransportConfiguration.StdioArgs))
        {
            OnPropertyChanged(nameof(CurrentEndpointDisplay));
        }
    }

    partial void OnSelectedTransportChanged(TransportOptionViewModel? value)
    {
        try
        {
            if (value == null)
            {
                return;
            }

            _transportConfig.SelectedTransportType = value.Type;
            OnPropertyChanged(nameof(SelectedTransportName));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to change transport type");
        }
    }

    public async Task ConnectToProfileAsync(ServerConfiguration? profile)
    {
        if (profile == null)
        {
            return;
        }

        try
        {
            await _connectionCommands.ConnectToAcpProfileAsync(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to profile {ProfileId}", profile.Id);
        }
        finally
        {
            OnPropertyChanged(nameof(AgentDisplayName));
        }
    }

    private string? ResolveConnectedProfileName()
    {
        var id = _preferences.LastSelectedServerId;
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return Profiles.Profiles.FirstOrDefault(p => p.Id == id)?.Name;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _transportConfig.PropertyChanged -= OnTransportConfigPropertyChanged;
        _connectionState.PropertyChanged -= OnChatPropertyChanged;
        Profiles.Profiles.CollectionChanged -= OnProfilesCollectionChanged;
        _preferences.PropertyChanged -= OnPreferencesPropertyChanged;
    }
}

public sealed class TransportOptionViewModel
{
    public TransportOptionViewModel(TransportType type, string name)
    {
        Type = type;
        Name = name;
    }

    public TransportType Type { get; }

    public string Name { get; }
}
