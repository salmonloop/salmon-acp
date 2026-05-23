namespace SalmonEgg.Presentation.Core.Services.Input;

public interface IGamepadDiagnosticsService
{
    GamepadDiagnosticsSnapshot GetCurrentSnapshot();
}
