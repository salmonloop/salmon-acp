namespace SalmonEgg.Presentation.Core.Services.Input;

public static class GamepadDirectionalSwitchMapper
{
    public static GamepadInputReading Apply(
        GamepadDirectionalSwitchPosition position,
        GamepadInputReading reading)
        => position switch
        {
            GamepadDirectionalSwitchPosition.Up => reading with { MoveUp = true },
            GamepadDirectionalSwitchPosition.UpRight => reading with { MoveUp = true, MoveRight = true },
            GamepadDirectionalSwitchPosition.Right => reading with { MoveRight = true },
            GamepadDirectionalSwitchPosition.DownRight => reading with { MoveDown = true, MoveRight = true },
            GamepadDirectionalSwitchPosition.Down => reading with { MoveDown = true },
            GamepadDirectionalSwitchPosition.DownLeft => reading with { MoveDown = true, MoveLeft = true },
            GamepadDirectionalSwitchPosition.Left => reading with { MoveLeft = true },
            GamepadDirectionalSwitchPosition.UpLeft => reading with { MoveUp = true, MoveLeft = true },
            _ => reading
        };
}
