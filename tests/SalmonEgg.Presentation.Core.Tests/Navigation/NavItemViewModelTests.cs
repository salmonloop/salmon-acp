using System.ComponentModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using SalmonEgg.Presentation.ViewModels.Navigation;
using Xunit;

namespace SalmonEgg.Presentation.Core.Tests.Navigation;

public sealed class NavItemViewModelTests
{
    [Fact]
    public void IsPaneClosed_Tracks_IsPaneOpen_And_Notifies()
    {
        var item = new DummyNavItem();
        var notified = false;
        item.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainNavItemViewModel.IsPaneClosed))
            {
                notified = true;
            }
        };

        item.IsPaneOpen = false;

        Assert.True(notified);
        Assert.False(item.IsPaneOpen);
        Assert.True(item.IsPaneClosed);
    }

    [Fact]
    public void SessionsHeader_Tracks_PaneState_For_Display()
    {
        var command = new AsyncRelayCommand(() => Task.CompletedTask);
        var header = new SessionsHeaderNavItemViewModel(command);
        var changed = new System.Collections.Generic.List<string>();

        header.PropertyChanged += (_, e) => changed.Add(e.PropertyName ?? string.Empty);

        header.IsPaneOpen = false;

        Assert.False(header.ShowHeaderLabel);
        Assert.True(header.ShowCompactButton);
        Assert.Contains(nameof(SessionsHeaderNavItemViewModel.ShowHeaderLabel), changed);
        Assert.Contains(nameof(SessionsHeaderNavItemViewModel.ShowCompactButton), changed);
    }

    private sealed class DummyNavItem : MainNavItemViewModel
    {
    }
}
