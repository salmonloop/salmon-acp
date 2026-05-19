using System;
using System.Collections.Generic;

namespace SalmonEgg.Presentation.Core.Services.Input;

public sealed class GamepadIntentProcessor
{
    public static readonly TimeSpan DefaultInitialRepeatDelay = TimeSpan.FromMilliseconds(350);
    public static readonly TimeSpan DefaultRepeatInterval = TimeSpan.FromMilliseconds(120);

    private const double ThumbstickDeadzone = 0.5;

    private readonly TimeSpan _initialRepeatDelay;
    private readonly TimeSpan _repeatInterval;
    private readonly Dictionary<GamepadNavigationIntent, PressState> _pressedStates = new();

    public GamepadIntentProcessor()
        : this(DefaultInitialRepeatDelay, DefaultRepeatInterval)
    {
    }

    public GamepadIntentProcessor(TimeSpan initialRepeatDelay, TimeSpan repeatInterval)
    {
        if (initialRepeatDelay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(initialRepeatDelay), initialRepeatDelay, "Initial repeat delay must be non-negative.");
        }

        if (repeatInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(repeatInterval), repeatInterval, "Repeat interval must be positive.");
        }

        _initialRepeatDelay = initialRepeatDelay;
        _repeatInterval = repeatInterval;
    }

    public IReadOnlyCollection<GamepadNavigationIntent> Process(GamepadInputReading reading, DateTimeOffset now)
    {
        var activeIntents = GetActiveIntents(reading);
        if (activeIntents.Count == 0)
        {
            _pressedStates.Clear();
            return Array.Empty<GamepadNavigationIntent>();
        }

        var raisedIntents = new List<GamepadNavigationIntent>();
        foreach (var intent in activeIntents)
        {
            if (!_pressedStates.TryGetValue(intent, out var state))
            {
                raisedIntents.Add(intent);
                _pressedStates[intent] = new PressState(now, now);
                continue;
            }

            if (now - state.PressedSince >= _initialRepeatDelay
                && now - state.LastRaised >= _repeatInterval)
            {
                raisedIntents.Add(intent);
                _pressedStates[intent] = state with { LastRaised = now };
            }
        }

        RemoveReleasedIntents(activeIntents);
        return raisedIntents;
    }

    public void Reset()
    {
        _pressedStates.Clear();
    }

    public static HashSet<GamepadNavigationIntent> GetActiveIntents(GamepadInputReading reading)
    {
        var intents = new HashSet<GamepadNavigationIntent>();

        if (reading.MoveUp)
        {
            intents.Add(GamepadNavigationIntent.MoveUp);
        }

        if (reading.MoveDown)
        {
            intents.Add(GamepadNavigationIntent.MoveDown);
        }

        if (reading.MoveLeft)
        {
            intents.Add(GamepadNavigationIntent.MoveLeft);
        }

        if (reading.MoveRight)
        {
            intents.Add(GamepadNavigationIntent.MoveRight);
        }

        AddThumbstickIntent(reading, intents);

        if (reading.Activate)
        {
            intents.Add(GamepadNavigationIntent.Activate);
        }

        if (reading.Back)
        {
            intents.Add(GamepadNavigationIntent.Back);
        }

        return intents;
    }

    private static void AddThumbstickIntent(GamepadInputReading reading, ISet<GamepadNavigationIntent> intents)
    {
        if (Math.Abs(reading.ThumbstickX) < ThumbstickDeadzone
            && Math.Abs(reading.ThumbstickY) < ThumbstickDeadzone)
        {
            return;
        }

        if (Math.Abs(reading.ThumbstickX) > Math.Abs(reading.ThumbstickY))
        {
            intents.Add(reading.ThumbstickX >= 0
                ? GamepadNavigationIntent.MoveRight
                : GamepadNavigationIntent.MoveLeft);
            return;
        }

        intents.Add(reading.ThumbstickY >= 0
            ? GamepadNavigationIntent.MoveUp
            : GamepadNavigationIntent.MoveDown);
    }

    private void RemoveReleasedIntents(IReadOnlySet<GamepadNavigationIntent> activeIntents)
    {
        var released = new List<GamepadNavigationIntent>();
        foreach (var pressedIntent in _pressedStates.Keys)
        {
            if (!activeIntents.Contains(pressedIntent))
            {
                released.Add(pressedIntent);
            }
        }

        foreach (var releasedIntent in released)
        {
            _pressedStates.Remove(releasedIntent);
        }
    }

    private readonly record struct PressState(DateTimeOffset PressedSince, DateTimeOffset LastRaised);
}
