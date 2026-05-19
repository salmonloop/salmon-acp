#if WINDOWS
using System;
using System.Collections.Generic;
using SalmonEgg.Presentation.Core.Services.Input;
using Windows.Gaming.Input;

namespace SalmonEgg.Presentation.Services.Input;

public sealed class WindowsRawGameControllerMapper
{
    private const double CenteredAxisValue = 0.5;

    public HashSet<GamepadNavigationIntent> GetActiveIntents(RawGameController controller)
    {
        return GamepadIntentProcessor.GetActiveIntents(GetInputReading(controller));
    }

    public GamepadInputReading GetInputReading(RawGameController controller)
    {
        ArgumentNullException.ThrowIfNull(controller);

        var buttons = new bool[controller.ButtonCount];
        var switches = new GameControllerSwitchPosition[controller.SwitchCount];
        var axes = new double[controller.AxisCount];
        controller.GetCurrentReading(buttons, switches, axes);

        var reading = default(GamepadInputReading);

        for (var i = 0; i < buttons.Length; i++)
        {
            if (!buttons[i])
            {
                continue;
            }

            reading = MapButtonLabel(controller.GetButtonLabel(i), reading);
        }

        for (var i = 0; i < switches.Length; i++)
        {
            var position = switches[i];
            if ((position & GameControllerSwitchPosition.Up) == GameControllerSwitchPosition.Up)
            {
                reading = reading with { MoveUp = true };
            }

            if ((position & GameControllerSwitchPosition.Down) == GameControllerSwitchPosition.Down)
            {
                reading = reading with { MoveDown = true };
            }

            if ((position & GameControllerSwitchPosition.Left) == GameControllerSwitchPosition.Left)
            {
                reading = reading with { MoveLeft = true };
            }

            if ((position & GameControllerSwitchPosition.Right) == GameControllerSwitchPosition.Right)
            {
                reading = reading with { MoveRight = true };
            }
        }

        if (axes.Length >= 2)
        {
            reading = reading with
            {
                ThumbstickX = axes[0] - CenteredAxisValue,
                ThumbstickY = CenteredAxisValue - axes[1]
            };
        }

        return reading;
    }

    private static GamepadInputReading MapButtonLabel(GameControllerButtonLabel label, GamepadInputReading reading)
    {
        return label switch
        {
            GameControllerButtonLabel.XboxUp or GameControllerButtonLabel.Up => reading with { MoveUp = true },
            GameControllerButtonLabel.XboxDown or GameControllerButtonLabel.Down => reading with { MoveDown = true },
            GameControllerButtonLabel.XboxLeft or GameControllerButtonLabel.Left => reading with { MoveLeft = true },
            GameControllerButtonLabel.XboxRight or GameControllerButtonLabel.Right => reading with { MoveRight = true },
            GameControllerButtonLabel.XboxA or GameControllerButtonLabel.Cross or GameControllerButtonLabel.LetterA => reading with { Activate = true },
            GameControllerButtonLabel.XboxB or GameControllerButtonLabel.Circle or GameControllerButtonLabel.LetterB or GameControllerButtonLabel.Back => reading with { Back = true },
            _ => reading
        };
    }
}
#endif
