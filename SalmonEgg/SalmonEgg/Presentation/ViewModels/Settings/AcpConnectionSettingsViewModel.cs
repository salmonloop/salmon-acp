using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SalmonEgg.Domain.Models;
using SalmonEgg.Presentation.ViewModels.Chat;

namespace SalmonEgg.Presentation.ViewModels.Settings;

public sealed partial class AcpConnectionSettingsViewModel : ObservableObject, IDisposable
{
    private readonly ILogger<AcpConnectionSettingsViewModel> _logger;
    private bool _disposed;
    private bool _isApplyingProfile;

    public ChatViewModel Chat { get; }
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

    public bool HasSelectedProfile => Profiles.SelectedProfile != null;

    public string SelectedProfileName => Profiles.SelectedProfile?.Name ?? string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanUpdateSelectedProfile))]
    private bool _isProfileDirty;

    public bool CanUpdateSelectedProfile => HasSelectedProfile && IsProfileDirty;

    public AcpConnectionSettingsViewModel(
        ChatViewModel chatViewModel,
        AcpProfilesViewModel profiles,
        ILogger<AcpConnectionSettingsViewModel> logger)
    {
        Chat = chatViewModel ?? throw new ArgumentNullException(nameof(chatViewModel));
        Profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        SelectedTransport = TransportOptions.FirstOrDefault(o => o.Type == Chat.TransportConfig.SelectedTransportType)
                            ?? TransportOptions.First();

        Chat.TransportConfig.PropertyChanged += OnTransportConfigPropertyChanged;
        Profiles.PropertyChanged += OnProfilesPropertyChanged;
        UpdateDirtyState();
    }

    private void OnProfilesPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Profiles.SelectedProfile))
        {
            ApplySelectedProfileToConfig();
        }
    }

    private void ApplySelectedProfileToConfig()
    {
        var profile = Profiles.SelectedProfile;
        if (profile == null)
        {
            UpdateDirtyState();
            return;
        }

        _isApplyingProfile = true;
        try
        {
            SelectedTransport = TransportOptions.FirstOrDefault(o => o.Type == profile.Transport) ?? TransportOptions.First();
            Chat.TransportConfig.SelectedTransportType = profile.Transport;

            if (profile.Transport == TransportType.Stdio)
            {
                Chat.TransportConfig.StdioCommand = profile.StdioCommand ?? string.Empty;
                Chat.TransportConfig.StdioArgs = profile.StdioArgs ?? string.Empty;
            }
            else
            {
                Chat.TransportConfig.RemoteUrl = profile.ServerUrl ?? string.Empty;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to apply selected profile to transport config");
        }
        finally
        {
            _isApplyingProfile = false;
            UpdateDirtyState();
            OnPropertyChanged(nameof(HasSelectedProfile));
            OnPropertyChanged(nameof(SelectedProfileName));
        }
    }

    private void OnTransportConfigPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Chat.TransportConfig.SelectedTransportType))
        {
            var current = TransportOptions.FirstOrDefault(o => o.Type == Chat.TransportConfig.SelectedTransportType);
            if (current != null && SelectedTransport?.Type != current.Type)
            {
                SelectedTransport = current;
            }
        }

        if (!_isApplyingProfile)
        {
            UpdateDirtyState();
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

            Chat.TransportConfig.SelectedTransportType = value.Type;
            OnPropertyChanged(nameof(SelectedTransportName));
            if (!_isApplyingProfile)
            {
                UpdateDirtyState();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to change transport type");
        }
    }

    private void UpdateDirtyState()
    {
        var profile = Profiles.SelectedProfile;
        if (profile == null)
        {
            IsProfileDirty = false;
            return;
        }

        var cfg = Chat.TransportConfig;
        var sameTransport = profile.Transport == cfg.SelectedTransportType;
        if (!sameTransport)
        {
            IsProfileDirty = true;
            return;
        }

        if (profile.Transport == TransportType.Stdio)
        {
            IsProfileDirty =
                !string.Equals((profile.StdioCommand ?? string.Empty).Trim(), (cfg.StdioCommand ?? string.Empty).Trim(), StringComparison.Ordinal) ||
                !string.Equals((profile.StdioArgs ?? string.Empty).Trim(), (cfg.StdioArgs ?? string.Empty).Trim(), StringComparison.Ordinal);
            return;
        }

        IsProfileDirty = !string.Equals((profile.ServerUrl ?? string.Empty).Trim(), (cfg.RemoteUrl ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase);
    }

    [RelayCommand]
    private async Task UpdateSelectedProfileFromCurrentAsync()
    {
        var profile = Profiles.SelectedProfile;
        if (profile == null)
        {
            return;
        }

        try
        {
            var updated = new ServerConfiguration
            {
                Id = profile.Id,
                Name = profile.Name,
                Transport = Chat.TransportConfig.SelectedTransportType,
                ServerUrl = Chat.TransportConfig.SelectedTransportType == TransportType.Stdio ? string.Empty : (Chat.TransportConfig.RemoteUrl ?? string.Empty),
                StdioCommand = Chat.TransportConfig.SelectedTransportType == TransportType.Stdio ? (Chat.TransportConfig.StdioCommand ?? string.Empty) : string.Empty,
                StdioArgs = Chat.TransportConfig.SelectedTransportType == TransportType.Stdio ? (Chat.TransportConfig.StdioArgs ?? string.Empty) : string.Empty,
                Authentication = profile.Authentication,
                Proxy = profile.Proxy,
                HeartbeatInterval = profile.HeartbeatInterval,
                ConnectionTimeout = profile.ConnectionTimeout
            };

            await Profiles.SaveAsync(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update selected profile from current config");
        }
        finally
        {
            UpdateDirtyState();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Chat.TransportConfig.PropertyChanged -= OnTransportConfigPropertyChanged;
        Profiles.PropertyChanged -= OnProfilesPropertyChanged;
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
