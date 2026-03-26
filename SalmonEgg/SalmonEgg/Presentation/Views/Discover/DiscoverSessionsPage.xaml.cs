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
    }

    private async void OnLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        SkeletonPulse?.Begin();
        await ViewModel.InitializeCommand.ExecuteAsync(null);
    }
}