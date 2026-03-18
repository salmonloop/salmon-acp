using System.ComponentModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using SalmonEgg.Presentation.Core.Services;
using SalmonEgg.Presentation.Services;
using SalmonEgg.Presentation.ViewModels.Navigation;
using Xunit;

namespace SalmonEgg.Presentation.Core.Tests.Navigation;

public sealed class NavItemViewModelTests
{
    [Fact]
    public void IsPaneClosed_Tracks_IsPaneOpen_And_Notifies()
    {
        var navState = new FakeNavigationPaneState();
        var item = new DummyNavItem(navState);
        var notified = false;
        item.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainNavItemViewModel.IsPaneClosed))
            {
                notified = true;
            }
        };

        navState.SetPaneOpen(true);
        navState.SetPaneOpen(false);

        Assert.True(notified);
        Assert.False(item.IsPaneOpen);
        Assert.True(item.IsPaneClosed);
    }

    [Fact]
    public void SessionsHeader_Tracks_PaneState_For_Display()
    {
        var navState = new FakeNavigationPaneState();
        var command = new AsyncRelayCommand(() => Task.CompletedTask);
        var header = new SessionsHeaderNavItemViewModel(command, navState);
        var changed = new System.Collections.Generic.List<string>();

        header.PropertyChanged += (_, e) => changed.Add(e.PropertyName ?? string.Empty);

        navState.SetPaneOpen(true);
        navState.SetPaneOpen(false);

        Assert.False(header.ShowHeaderLabel);
        Assert.True(header.ShowCompactButton);
        Assert.Contains(nameof(SessionsHeaderNavItemViewModel.ShowHeaderLabel), changed);
        Assert.Contains(nameof(SessionsHeaderNavItemViewModel.ShowCompactButton), changed);
    }

    private sealed class DummyNavItem : MainNavItemViewModel
    {
        public DummyNavItem(INavigationPaneState navState) : base(navState) { }
    }

    private sealed class FakeNavigationPaneState : INavigationPaneState
    {
        public bool IsPaneOpen { get; private set; }
        public event EventHandler? PaneStateChanged;

        public void SetPaneOpen(bool isOpen)
        {
            if (IsPaneOpen == isOpen)
            {
                return;
            }

            IsPaneOpen = isOpen;
            PaneStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
