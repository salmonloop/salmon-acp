using System;

namespace SalmonEgg.Presentation.Services;

public interface INavigationPaneState
{
    bool IsPaneOpen { get; }
    event EventHandler? PaneStateChanged;
}
