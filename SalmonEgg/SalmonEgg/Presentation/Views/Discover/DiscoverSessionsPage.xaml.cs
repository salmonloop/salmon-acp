using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using SalmonEgg.Presentation.ViewModels.Discover;

namespace SalmonEgg.Presentation.Views.Discover;

public sealed partial class DiscoverSessionsPage : Page
{
    public DiscoverSessionsViewModel ViewModel { get; }

    public DiscoverSessionsPage()
    {
        this.InitializeComponent();
        ViewModel = App.ServiceProvider.GetRequiredService<DiscoverSessionsViewModel>();
        DataContext = ViewModel;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        SkeletonPulse?.Begin();
        if (ViewModel.InitializeCommand.CanExecute(null))
        {
            await ViewModel.InitializeCommand.ExecuteAsync(null);
        }
    }

    private void OnUnloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ViewModel.Dispose();
    }
}