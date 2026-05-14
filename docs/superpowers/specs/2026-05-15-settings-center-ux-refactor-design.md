# Settings Center UX Refactor Design

## Context

SalmonEgg settings are used by developers and operators who repeatedly tune ACP profiles, local path mappings, storage behavior, diagnostics, shortcuts, and app preferences. The product direction is precise, quiet, capable, and native-first. The refactor must keep WinUI 3 and Uno native controls as behavior owners, keep ViewModels as the source of truth, and avoid template replacement or pointer-level compensation.

The approved direction keeps the current top settings navigation (`NavigationView` with `PaneDisplayMode="Top"`) and improves the page interiors.

## Goals

1. Keep the current top settings category navigation.
2. Add a consistent page title and short scope summary to every settings subpage.
3. Replace ad hoc page interiors with a consistent settings-row pattern: label, optional description, native control/action.
4. Stabilize command placement: primary section commands near section headers, secondary commands in row-level action areas, destructive commands isolated.
5. Use progressive disclosure for advanced, diagnostic, and dangerous surfaces.
6. Preserve WinUI/Uno native control behavior and MVVM ownership.

## Non-Goals

1. Do not replace the top settings navigation with a left navigation shell.
2. Do not introduce a custom settings control that reimplements native selection, focus, toggle, combo box, or list behavior.
3. Do not change settings persistence, ACP protocol behavior, connection state ownership, or navigation ownership.
4. Do not rewrite whole pages from scratch when scoped layout edits can achieve the goal.
5. Do not introduce new platform-specific behavior in shared ViewModels or business logic.

## Current UX Problems

### Organization

Settings pages mix scopes unevenly. ACP profiles, path mappings, and session loading policy live in one long page without a strong page summary or clear hierarchy. Data storage groups routine cache/export actions and danger actions reasonably, but command clusters are visually similar. Diagnostics combines environment facts, log directory actions, live log viewing, connection state, and diagnostic bundle generation without enough distinction between passive facts and active tools.

### Layout

Most pages repeat a manual `TextBlock` heading plus `Border` container pattern. Headings use accent color for section identity, which makes many sections compete visually. Some pages use nested `StackPanel` layouts where a row-based `Grid` would produce more stable alignment. Command buttons are often horizontal stacks that may wrap poorly on narrow windows.

### Components

Native controls are mostly used, which is good. The main issue is composition: controls are placed directly in hand-rolled containers instead of consistent settings rows. `Expander` is already used for live logs; the refactor should extend that principle only where progressive disclosure is meaningful. The existing app-level `SubtleButtonStyle` overrides the `Button` template; future settings work should prefer native `Button` styling or lightweight styling resources rather than relying on template replacement.

## Proposed Design

### Shell

Keep `SettingsShellPage` using top `NavigationView`. The shell remains responsible only for selecting a settings section and navigating the inner `Frame`. No new state owner is introduced.

Recommended shell polish:

- Keep `AlwaysShowHeader="False"`, `IsSettingsVisible="False"`, and `PaneDisplayMode="Top"`.
- Keep the inner `Frame` and motion policy.
- Tune spacing around the top navigation and content frame only through shared resources if needed.

### Shared Page Pattern

Each settings subpage uses:

1. Page title.
2. One-line scope summary.
3. A vertical sequence of settings sections.
4. Each section contains consistent rows.

Rows use native controls:

- Boolean preferences use `ToggleSwitch`.
- Enumerated preferences use `ComboBox`.
- Numeric preferences use `NumberBox`.
- Collections use `ListView` with native selection semantics.
- Disclosure uses `Expander`.
- Feedback uses `InfoBar`.

The XAML pattern should stay simple and transparent. A small shared style/resource set is acceptable for spacing and text hierarchy. A custom container control is only acceptable if it is a thin content presenter and does not intercept input or change native control behavior.

### Row Layout

Routine rows should use a shallow `Grid`:

- Column 0: title and description.
- Column 1: native control or command group.
- Row spacing comes from shared resources.
- Text wraps in the description column.
- Controls keep stable widths where native controls expect them (`ComboBox`, `NumberBox`, shortcut recorder).

This avoids repeated nested `StackPanel` patterns and keeps labels aligned across pages.

### Page-Specific Structure

#### General

Sections:

- Startup and window
- Language

Rows:

- Launch on startup
- Minimize to tray
- App language

Unsupported platform messages remain driven by capability properties on the ViewModel. They should stay near the affected row and not become a global warning.

#### Appearance

Sections:

- Theme
- Interaction
- Window backdrop

Rows:

- Theme selection
- Global transition animation
- Background material

The informational notice remains native `InfoBar`, but should be visually secondary to settings rows. It can move to the top or bottom depending on final page flow.

#### ACP / Agent

Sections:

- ACP profiles
- Local path mappings
- Session loading policy

Profiles stay in a native `ListView`. The profile row should separate profile identity, status, connect action, and overflow commands more clearly. Primary commands such as refresh and new profile stay in the section header action area.

Path mappings stay ViewModel-driven through `PathMappingRows`. The add command remains disabled when no profile is selected. Rows should align the remote and local paths predictably and keep remove as a row-level command.

Hydration policy remains an advanced/protocol setting. It can be visually de-emphasized or placed under an `Expander` labeled as advanced if the page becomes crowded.

#### Data And Storage

Sections:

- Privacy
- Cache
- Export
- Dangerous actions

Dangerous actions should use `Expander` or a clearly separated section body so routine cache/export work does not sit at the same visual priority as destructive operations. Confirmation behavior remains in ViewModel/service interactions and code-behind only invokes commands or UI service calls already present.

#### Shortcuts

Sections:

- Custom shortcuts

Conflict and invalid-state `InfoBar` elements remain at the top. Shortcut rows use stable columns: command name/default gesture, recorder, restore command. Row layout must preserve native text entry behavior and not override input handling beyond the existing `ShortcutRecorder`.

#### Diagnostics

Sections:

- Environment
- Logs
- Connection diagnostics
- Diagnostic bundle

Environment and connection diagnostics are read-only facts and should use compact key/value rows. Log actions are tools and should use command rows. Live log viewer remains behind native `Expander` and must keep unload cleanup behavior.

#### About

Sections:

- App information
- Support
- Open source acknowledgements

App information and support become key/value or command rows. Open source acknowledgements remain a native `ListView` with generated data and no runtime binding fallback.

## Accessibility And Localization

- Keep all user-visible XAML text under `x:Uid` or existing resource patterns.
- Add or preserve stable `AutomationProperties.AutomationId` values where tests or GUI smoke need them.
- Do not replace native focus visuals or selection visuals.
- Keep keyboard navigation owned by native controls.
- Ensure descriptions wrap and do not overlap controls on narrow widths.

## Implementation Plan Outline

1. Add shared settings page styles/resources for page title, page summary, section title, section container, row title, row description, and row spacing.
2. Update each subpage XAML to use page title, summary, section containers, and settings rows.
3. Refine ACP profile, path mapping, shortcut, diagnostics, and about list layouts while preserving native `ListView` behavior.
4. Add or update XAML compliance tests that verify:
   - top navigation remains top navigation;
   - pages include page-level titles/summaries;
   - path mapping remains ViewModel-driven;
   - dangerous storage actions remain separated;
   - live log viewer remains behind `Expander`;
   - generated acknowledgements remain `x:Bind`-driven.
5. Run targeted Presentation Core tests and a build appropriate to XAML/resource changes.

## Risks And Constraints

The largest risk is accidentally turning visual polish into behavioral ownership. The mitigation is to keep changes in XAML layout and shared resources, with code-behind limited to existing navigation and command invocation paths.

The existing `SubtleButtonStyle` uses a full `ControlTemplate`, which conflicts with the long-term native-first direction. This refactor should avoid expanding that pattern and should prefer native button styles or lightweight resource overrides in future settings-specific changes.

Because this is a non-document-only future implementation, final delivery must run relevant tests and build verification. The design document itself is documentation-only.

