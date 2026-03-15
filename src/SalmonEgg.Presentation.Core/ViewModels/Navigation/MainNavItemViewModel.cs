using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SalmonEgg.Presentation.ViewModels.Navigation;

public abstract partial class MainNavItemViewModel : ObservableObject
{
    public ObservableCollection<MainNavItemViewModel> Children { get; } = new();

    private bool _isPaneOpen = true;

    public bool IsPaneOpen
    {
        get => _isPaneOpen;
        set
        {
            if (SetProperty(ref _isPaneOpen, value))
            {
                OnPropertyChanged(nameof(IsPaneClosed));
                OnPaneStateChanged();
            }
        }
    }

    public bool IsPaneClosed => !IsPaneOpen;

    protected virtual void OnPaneStateChanged()
    {
    }
}

public sealed partial class SessionsHeaderNavItemViewModel : MainNavItemViewModel
{
    public string Title { get; } = "会话";

    public IAsyncRelayCommand AddProjectCommand { get; }

    public bool ShowHeaderLabel => IsPaneOpen;

    public bool ShowCompactButton => IsPaneClosed;

    public SessionsHeaderNavItemViewModel(IAsyncRelayCommand addProjectCommand)
    {
        AddProjectCommand = addProjectCommand;
    }

    protected override void OnPaneStateChanged()
    {
        OnPropertyChanged(nameof(ShowHeaderLabel));
        OnPropertyChanged(nameof(ShowCompactButton));
    }
}
