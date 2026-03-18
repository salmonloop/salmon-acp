using System;

namespace SalmonEgg.Presentation.Services;

using SalmonEgg.Presentation.Core.Mvux.ShellLayout;

/// <summary>
/// Service for managing the state of the right sidebar (SSOT).
/// </summary>
public interface IRightPanelService
{
    RightPanelMode CurrentMode { get; }
    event EventHandler? ModeChanged;

    double PanelWidth { get; }
    event EventHandler? WidthChanged;
}
