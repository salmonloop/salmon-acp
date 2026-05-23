using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using Microsoft.Extensions.Logging;
using SalmonEgg.Presentation.Core.Services.Input;

namespace SalmonEgg.Presentation.Services.Input;

public sealed class GuiGamepadInputService : IGamepadInputService
{
    private const string GuiEnabledEnvVar = "SALMONEGG_GUI";
    private const string GuiControlFileEnvVar = "SALMONEGG_GUI_CONTROL_FILE";
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(50);

    private readonly ILogger<GuiGamepadInputService> _logger;
    private Timer? _timer;
    private string? _lastCommandId;
    private bool _disposed;

    public GuiGamepadInputService(ILogger<GuiGamepadInputService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public event EventHandler<GamepadNavigationIntent>? IntentRaised;

    public void Start()
    {
        ThrowIfDisposed();
        if (!IsGuiAutomationEnabled() || string.IsNullOrWhiteSpace(GetControlFilePath()))
        {
            return;
        }

        _timer ??= new Timer(PollControlFile, null, TimeSpan.Zero, PollInterval);
    }

    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Stop();
        _disposed = true;
    }

    private void PollControlFile(object? state)
    {
        var controlFilePath = GetControlFilePath();
        if (string.IsNullOrWhiteSpace(controlFilePath) || !File.Exists(controlFilePath))
        {
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(controlFilePath));
            var root = document.RootElement;
            if (!TryReadString(root, "kind", out var kind)
                || !string.Equals(kind, "gamepad-intent", StringComparison.Ordinal))
            {
                return;
            }

            if (!TryReadString(root, "id", out var id)
                || string.Equals(id, _lastCommandId, StringComparison.Ordinal))
            {
                return;
            }

            if (!TryReadString(root, "intent", out var intentText)
                || !Enum.TryParse<GamepadNavigationIntent>(intentText, ignoreCase: true, out var intent))
            {
                _logger.LogWarning("GUI gamepad control file contained an invalid intent.");
                _lastCommandId = id;
                return;
            }

            _lastCommandId = id;
            IntentRaised?.Invoke(this, intent);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            _logger.LogDebug(ex, "GUI gamepad control file was not ready to read.");
        }
    }

    private static bool IsGuiAutomationEnabled()
        => string.Equals(Environment.GetEnvironmentVariable(GuiEnabledEnvVar), "1", StringComparison.Ordinal);

    private static string? GetControlFilePath()
        => Environment.GetEnvironmentVariable(GuiControlFileEnvVar);

    private static bool TryReadString(JsonElement element, string propertyName, out string value)
    {
        value = string.Empty;
        if (!element.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(value);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
