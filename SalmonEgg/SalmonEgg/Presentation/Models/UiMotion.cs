using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media.Animation;

namespace SalmonEgg.Presentation.Models;

public sealed partial class UiMotion : ObservableObject
{
    public static UiMotion Current { get; } = new();

    private bool _isAnimationEnabled = true;

    public bool IsAnimationEnabled
    {
        get => _isAnimationEnabled;
        set
        {
            if (SetProperty(ref _isAnimationEnabled, value))
            {
                OnPropertyChanged(nameof(PageTransitions));
                OnPropertyChanged(nameof(NavItemTransitions));
                OnPropertyChanged(nameof(ListItemTransitions));
            }
        }
    }

    public TransitionCollection? PageTransitions =>
        IsAnimationEnabled ? CreateEntranceTransitions(0, 12) : null;

    public TransitionCollection? NavItemTransitions =>
        IsAnimationEnabled ? CreateEntranceTransitions(8, 0) : null;

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
