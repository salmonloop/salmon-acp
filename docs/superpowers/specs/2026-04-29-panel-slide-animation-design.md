# Panel Slide-from-Edge Animation Design

**Date:** 2026-04-29
**Status:** Design
**Target:** Bottom Panel + Right Panel (MainPage)

## Motivation

Toggling the Bottom Panel and Right Panel currently snaps between visible and hidden states with no motion. WinUI 3 / Fluent Design prescribes `Slide from Edge + Fade` for auxiliary panes: panels should slide in from their adjacent edge with a concurrent opacity transition.

## Non-Goals

- ConnectedAnimation cross-page coordination
- Vertical edge animations (panels always come from right / bottom)
- Animating NavigationView Pane (already handled natively by the control)

## Design

### Insertion Point

Animation sits in `MainPage` code-behind, between `ShellLayoutViewModel.PropertyChanged` notification and the actual `Visibility` mutation on the panel element. The SSOT pipeline (`Action → Reducer → Policy → Snapshot → ViewModel`) is untouched.

```
LayoutVM.PropertyChanged (RightPanelVisible / BottomPanelVisible)
  → MainPage.OnLayoutViewModelPropertyChanged
    → IsAnimationEnabled?
      ├─ Yes → AnimatePanelOpen/Close()  [Storyboard + TranslateTransform]
      └─ No  → direct Visibility switch (existing behavior)
```

### Panel XAML Changes

Each panel gets a `TranslateTransform` and loses the `Visibility` x:Bind (visibility is now driven by code-behind):

**Right Panel** (already has `RightPanelTranslate`):
```xml
<Grid x:Name="RightPanelColumn" ...>
    <!-- Remove: Visibility="{x:Bind LayoutVM.RightPanelVisible, ...}" -->
    <Grid.RenderTransform>
        <TranslateTransform x:Name="RightPanelTranslate" />
    </Grid.RenderTransform>
    ...
</Grid>
```

**Bottom Panel** (adds `BottomPanelTranslate`):
```xml
<chatViews:BottomPanelHost x:Name="BottomPanelHost" ...>
    <!-- Remove: Visibility="{x:Bind LayoutVM.BottomPanelVisible, ...}" -->
    <chatViews:BottomPanelHost.RenderTransform>
        <TranslateTransform x:Name="BottomPanelTranslate" />
    </chatViews:BottomPanelHost.RenderTransform>
    ...
</chatViews:BottomPanelHost>
```

### Storyboards (4 total, in Page.Resources)

Fluent Design "Fast" duration token: **167ms**.

| Storyboard | Target | Property | From | To | Easing |
|---|---|---|---|---|---|
| `RightPanelSlideIn` | `RightPanelTranslate.X` | `DoubleAnimation` | panelWidth | 0 | `CubicEaseOut` |
| | `RightPanelColumn.Opacity` | `DoubleAnimation` | 0 | 1 | — |
| `RightPanelSlideOut` | `RightPanelTranslate.X` | `DoubleAnimation` | 0 | panelWidth | `CubicEaseIn` |
| | `RightPanelColumn.Opacity` | `DoubleAnimation` | 1 | 0 | — |
| `BottomPanelSlideUp` | `BottomPanelTranslate.Y` | `DoubleAnimation` | panelHeight | 0 | `CubicEaseOut` |
| | `BottomPanelHost.Opacity` | `DoubleAnimation` | 0 | 1 | — |
| `BottomPanelSlideDown` | `BottomPanelTranslate.Y` | `DoubleAnimation` | 0 | panelHeight | `CubicEaseIn` |
| | `BottomPanelHost.Opacity` | `DoubleAnimation` | 1 | 0 | — |

Open = EaseOut (smooth arrival). Close = EaseIn (fast departure). This asymmetry is the Fluent convention.

The `To` values for SlideOut are set from a cached `_lastPanelWidth` / `_lastPanelHeight` field at animation start, because `RightPanelTranslate.X` is currently 0 when the close animation fires.

### Code-behind State Fields

| Field | Purpose |
|---|---|
| `_isRightPanelAnimating` | Guard against re-entrant PropertyChanged during animation |
| `_isBottomPanelAnimating` | Same for bottom panel |
| `_lastRightPanelWidth` | Close-animation slide-out distance |
| `_lastBottomPanelHeight` | Close-animation slide-out distance |
| `_pendingRightPanelToggle` | If a reverse toggle arrives mid-animation, queue it |
| `_pendingBottomPanelToggle` | Same for bottom panel |

### Animation Logic Flow

```
AnimateRightPanelOpen():
  if !IsAnimationEnabled → RightPanelColumn.Visibility = Visible; return
  cache _lastRightPanelWidth from LayoutVM.RightPanelWidth
  set RightPanelTranslate.X = _lastRightPanelWidth  // offscreen start
  RightPanelColumn.Opacity = 0
  RightPanelColumn.Visibility = Visible
  RightPanelSlideIn.Children[0] // DoubleAnimation.To = 0
  RightPanelSlideIn.Begin()
  _isRightPanelAnimating = true

AnimateRightPanelClose():
  if !IsAnimationEnabled → RightPanelColumn.Visibility = Collapsed; return
  Read cached _lastRightPanelWidth
  RightPanelSlideOut.Children[0] // DoubleAnimation.To = _lastRightPanelWidth
  RightPanelSlideOut.Begin()
  _isRightPanelAnimating = true
  // Storyboard.Completed → RightPanelColumn.Visibility = Collapsed; _isRightPanelAnimating = false

// BottomPanel follows the identical pattern with Y axis and _lastBottomPanelHeight
```

### Edge Cases

| Scenario | Handling |
|---|---|
| Toggle during open animation | Set `_pendingToggle = true`. Completed handler reverses direction. |
| Toggle during close animation | Same — completed handler re-opens with updated position. |
| Navigate away from chat | `ResetChatAuxiliaryPanelsOnChatExit()` calls direct Collapse, no animation. |
| Window resize during animation | Unlikely within 167ms. `_lastPanelWidth`/`Height` is a snapshot frozen at animation start. |
| `IsAnimationEnabled` toggled mid-animation | Running Storyboard is not interrupted (Composition thread). Future toggles use new setting. |
| First open (no cached size) | `_lastPanelWidth`/`Height` are zero-initialized — gated by `Visible == false → true`, which always sets the cache from current `LayoutVM.RightPanelWidth`/`BottomPanelHeight`. |
| Grid length animation | Not animated. `RightPanelWidth`/`BottomPanelHeight` bindings snap instantly. The Transform overrides the visual position, then snaps the layout column in the same frame, which is invisible under the sliding surface. |

## Implementation File Plan

| File | Change |
|---|---|
| `MainPage.xaml` | Remove `Visibility` x:Bind from both panels. Add `BottomPanelTranslate` Transform. Add 4 Storyboards to `Page.Resources`. |
| `MainPage.xaml.cs` | Add `OnLayoutViewModelPropertyChanged` gating for panel visibility changes. Add animate open/close methods and `Completed` handlers. Add guard and cache fields. |

## Testing

- **Unit**: Verify `ShellLayoutPolicy.Compute()` snapshots are unchanged (no regression).
- **GUI smoke**: Toggle Bottom Panel → verify slide-up + fade (167ms). Toggle Right Panel (Diff/Todo) → verify slide-left + fade. Toggle both in quick succession → verify no crash, final state correct.
- **A11y**: Collapsed panels are removed from accessibility tree; no animation introduces stale focus.
- **Performance**: Storyboards run on the Composition thread; no UI-thread layout re-entrance.

## References

- [Fluent Design motion guidance — Duration tokens](https://learn.microsoft.com/en-us/windows/apps/design/signature-experiences/motion#timing-and-easing)
- WinUI 3 `NavigationView.PaneThemeTransition` (edge-slide native behavior)
