namespace SalmonEgg.Presentation.Core.Services.Input;

public sealed class NoOpGamepadDiagnosticsService : IGamepadDiagnosticsService
{
    public GamepadDiagnosticsSnapshot GetCurrentSnapshot()
        => GamepadDiagnosticsSnapshot.Unsupported;
}
