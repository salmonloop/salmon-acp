using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using SalmonEgg.Domain.Models;
using SalmonEgg.Presentation.ViewModels.Chat;

namespace SalmonEgg.Presentation.ViewModels.Settings;

public sealed partial class AcpConnectionSettingsViewModel : ObservableObject, IDisposable
{
    private readonly ILogger<AcpConnectionSettingsViewModel> _logger;
    private bool _disposed;

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
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to change transport type");
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
