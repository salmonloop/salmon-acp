using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media.Animation;

namespace SalmonEgg.Presentation.Models;

public sealed partial class UiMotion : ObservableObject
{
    public static UiMotion Current { get; } = new();

    private bool _isAnimationEnabled = true;

    /// <summary>
    /// SSOT for whether animations are globally enabled.
    /// </summary>
    public bool IsAnimationEnabled
    {
        get => _isAnimationEnabled;
        set
        {
            if (SetProperty(ref _isAnimationEnabled, value))
            {
                // Notify that all transition properties might have changed (from null to collection or vice versa)
                OnPropertyChanged(nameof(PageTransitions));
                OnPropertyChanged(nameof(NavItemTransitions));
                OnPropertyChanged(nameof(ListItemTransitions));
            }
        }
    }

    /// <summary>
    /// Entrance transitions for pages. Returning a new collection each time avoids 
    /// "element already has a parent" or thread-affinity issues in WinUI 3.
    /// </summary>
    public TransitionCollection? PageTransitions =>
        IsAnimationEnabled ? CreateEntranceTransitions(0, 12) : null;

    /// <summary>
    /// Entrance transitions for sidebar items.
    /// </summary>
    public TransitionCollection? NavItemTransitions =>
        IsAnimationEnabled ? CreateEntranceTransitions(8, 0) : null;

    /// <summary>
    /// Standard list add/remove/reposition transitions.
    /// </summary>
    public TransitionCollection? ListItemTransitions =>
        IsAnimationEnabled ? CreateListTransitions() : null;

    private static TransitionCollection CreateEntranceTransitions(double fromHorizontal, double fromVertical)
    {
        return new TransitionCollection
        {
            new EntranceThemeTransition
            {
                FromHorizontalOffset = fromHorizontal,
                FromVerticalOffset = fromVertical
            }
        };
    }

    private static TransitionCollection CreateListTransitions()
    {
        return new TransitionCollection
        {
            new AddDeleteThemeTransition(),
            new RepositionThemeTransition()
        };
    }
}
