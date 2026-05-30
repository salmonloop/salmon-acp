namespace SalmonEgg.Presentation.Core.Services.Input;

public readonly record struct GamepadInputPathTransition(
    GamepadInputPath Path,
    bool Changed);

public sealed class GamepadInputPathTracker
{
    public GamepadInputPath CurrentPath { get; private set; } = GamepadInputPath.None;

    public GamepadInputPathTransition Apply(bool hasActiveReading, GamepadInputPath path)
    {
        var nextPath = hasActiveReading ? path : GamepadInputPath.None;
        if (CurrentPath == nextPath)
        {
            return new GamepadInputPathTransition(nextPath, Changed: false);
        }

        CurrentPath = nextPath;
        return new GamepadInputPathTransition(nextPath, Changed: true);
    }

    public GamepadInputPathTransition Reset()
    {
        return Apply(hasActiveReading: false, GamepadInputPath.None);
    }
}
