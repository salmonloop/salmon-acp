using System;
using SalmonEgg.Presentation.ViewModels.Navigation;

namespace SalmonEgg.Presentation.Services;

public sealed class RightPanelService : IRightPanelService
{
    private RightPanelMode _currentMode = RightPanelMode.None;
    private double _panelWidth = 320;

    public RightPanelMode CurrentMode
    {
        get => _currentMode;
        set
        {
            if (_currentMode != value)
            {
                _currentMode = value;
                ModeChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public event EventHandler? ModeChanged;

    public double PanelWidth
    {
        get => _panelWidth;
        set
        {
            if (!double.Equals(_panelWidth, value))
            {
                _panelWidth = value;
                WidthChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public event EventHandler? WidthChanged;
}
