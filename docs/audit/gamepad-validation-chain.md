# Gamepad Validation Chain

This document defines what each validation layer is allowed to prove for gamepad support on Windows.

## Why the split exists

Microsoft's guidance says:

- directional keyboard support is the baseline for gamepad and remote behavior;
- after keyboard focus navigation is correct, validate again with gamepad/remote to find weak spots;
- physical gamepads are also available through `Windows.Gaming.Input` polling APIs.

That means one automated path is not enough to prove all gamepad behavior.

## Validation layers

### 1. Core contract tests

Purpose:

- prove local focus contracts such as `XYFocus*`, content entry targets, startup focus seeding, and local navigation policies.

Examples:

- `NavigationCoreTests`
- `XamlComplianceTests`

What this layer can prove:

- the intended native focus graph exists in source;
- app-level policy does not reintroduce forbidden fallback/state-machine behavior.

What this layer cannot prove:

- real UIA focus movement;
- physical controller polling;
- system window activation timing.

### 2. Keyboard GUI smoke

Purpose:

- prove WinUI/Uno native XY focus behavior on the installed MSIX build.

Examples:

- Start page `PromptBox -> Up -> first suggestion`
- Start page `PromptBox -> Down -> selector row`

What this layer can prove:

- the installed build exposes the intended focusable controls;
- native arrow-key navigation reaches the expected targets;
- focus visuals and real container topology are coherent enough for UIA observation.

What this layer cannot prove:

- `Windows.Gaming.Input` polling;
- physical controller transport;
- device-specific timing around controller connect/disconnect.

### 3. Synthetic gamepad-key GUI smoke

Backend:

- default `SALMONEGG_GUI_GAMEPAD_INPUT_BACKEND=synthetic`

Mechanism:

- sends `Windows.System.VirtualKey` gamepad keys such as `GamepadDPadDown`, `GamepadA`, `GamepadB`.

Purpose:

- prove the app's gamepad-key routing path when gamepad input is represented as key events.

What this layer can prove:

- focused controls and app-level handlers respond coherently to gamepad virtual keys;
- no duplicate shell fallback path is fighting the native key route.

What this layer cannot prove:

- physical controller polling through `Windows.Gaming.Input`;
- whether a real handpad reaches the app through the same chain;
- any claim that depends on raw/native controller state.

Rule:

- do not use synthetic `VirtualKey` smokes as the only gate for physical-controller regressions.

### 4. Native-device bridge GUI smoke

Backend:

- `SALMONEGG_GUI_GAMEPAD_INPUT_BACKEND=native-device`
- `SALMONEGG_GUI_GAMEPAD_NATIVE_BRIDGE=<bridge exe>`

Purpose:

- exercise the `Windows.Gaming.Input` path with a controller-like native device bridge.

What this layer can prove:

- diagnostics or app behavior that depends on the native polling path sees controller input;
- bridge-backed D-pad/A/B reach the application as controller input instead of only as keyboard-like virtual keys.

What this layer cannot prove:

- every real physical controller model behaves identically;
- shell/system permission windows preserve focus the same way as a real user environment.

Rule:

- keep these tests narrow and explicit; only use them where native polling is the behavior under test.

### 5. Real controller manual smoke

Purpose:

- final authority for product claims about physical handpad behavior.

Required for:

- startup focus visibility;
- permission windows or other system-owned UI interruptions;
- any case where synthetic or native-bridge results conflict with observed real-device behavior;
- cases that depend on real controller timing or hardware state.

Minimum manual checklist:

1. cold start first-focus behavior;
2. main navigation to content entry;
3. content-to-navigation return path;
4. selector/value controls do not change unexpectedly;
5. system-owned interruption paths (for example microphone permission) return to a visible, coherent focus target.

## Test selection rules

1. If the bug is about focus topology or `XYFocus*`, write/keep a core contract test and a keyboard GUI smoke first.
2. If the bug is about gamepad virtual-key routing, synthetic GUI smoke is allowed.
3. If the bug is about physical controller polling, native-device smoke or real manual validation is required.
4. If synthetic smoke conflicts with real controller behavior, treat the validation chain as suspect first. Do not modify product logic until the contradiction is resolved.
5. Start page and similar focus-entry scenarios should default to keyboard GUI smoke unless the specific regression is proven to live only in the controller polling path.
6. Do not force keyboard-arrow GUI smokes onto selector/value controls whose native collapsed-state arrow behavior is selection-oriented rather than focus-escape-oriented; cover those paths with source contracts plus native-device or manual gamepad validation instead.

## Microsoft Learn references

- Gamepad and remote control interactions:
  https://learn.microsoft.com/en-us/windows/uwp/ui-input/gamepad-and-remote-interactions
- Keyboard interactions:
  https://learn.microsoft.com/en-us/windows/apps/develop/input/keyboard-interactions
- Focus navigation without a mouse:
  https://learn.microsoft.com/en-us/windows/apps/develop/input/focus-navigation
- Gamepad and vibration:
  https://learn.microsoft.com/en-us/windows/uwp/gaming/gamepad-and-vibration
- UI navigation controller:
  https://learn.microsoft.com/en-us/windows/uwp/gaming/ui-navigation-controller
- Input injection:
  https://learn.microsoft.com/en-us/windows/uwp/ui-input/input-injection
