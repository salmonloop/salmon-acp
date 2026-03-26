using System;
using System.ComponentModel;
using System.Threading.Tasks;
using SalmonEgg.Application.Services.Chat;
using SalmonEgg.Domain.Models;
using SalmonEgg.Presentation.ViewModels.Chat;

namespace SalmonEgg.Presentation.Core.Services.Chat;

public interface IDiscoverSessionsConnectionFacade : INotifyPropertyChanged
{
    bool IsConnecting { get; }

    bool IsInitializing { get; }

    bool IsConnected { get; }

    string? ConnectionErrorMessage { get; }

    IChatService? CurrentChatService { get; }

    Task ConnectToProfileAsync(ServerConfiguration profile);
}

public sealed class DiscoverSessionsConnectionFacade : IDiscoverSessionsConnectionFacade
{
    private readonly ChatViewModel _chatViewModel;

    public DiscoverSessionsConnectionFacade(ChatViewModel chatViewModel)
    {
        _chatViewModel = chatViewModel ?? throw new ArgumentNullException(nameof(chatViewModel));
    }

    public event PropertyChangedEventHandler? PropertyChanged
    {
        add => _chatViewModel.PropertyChanged += value;
        remove => _chatViewModel.PropertyChanged -= value;
    }

    public bool IsConnecting => _chatViewModel.IsConnecting;

    public bool IsInitializing => _chatViewModel.IsInitializing;

    public bool IsConnected => _chatViewModel.IsConnected;

    public string? ConnectionErrorMessage => _chatViewModel.ConnectionErrorMessage;

    public IChatService? CurrentChatService => _chatViewModel.CurrentChatService;

    public Task ConnectToProfileAsync(ServerConfiguration profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return _chatViewModel.ConnectToAcpProfileCommand.ExecuteAsync(profile);
    }
}
