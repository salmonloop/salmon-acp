# ACP Profile List UX Refinement Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refine the ACP settings profile list so profile identity, endpoint, status, connect action, and overflow actions are easier to scan while preserving native WinUI/Uno behavior.

**Architecture:** Keep the existing `AcpConnectionSettingsPage.xaml` ViewModel bindings and native controls. Add focused XAML contract assertions first, then update only the profile section row layout. Do not add code-behind, ViewModel state, custom templates, or platform-specific branches.

**Tech Stack:** WinUI 3 / Uno XAML, `x:Bind`, native `ListView`, `ToggleSwitch`, `Button.Flyout`, xUnit Presentation Core tests, .NET build tooling.

---

## File Structure

- Modify: `tests/SalmonEgg.Presentation.Core.Tests/Settings/AcpConnectionSettingsXamlTests.cs`
  - Responsibility: contract tests that preserve native controls, bindings, commands, and scope boundaries for the ACP settings page.
- Modify: `SalmonEgg/SalmonEgg/Presentation/Views/Settings/AcpConnectionSettingsPage.xaml`
  - Responsibility: profile section presentation only. Preserve data flow and command/event entry points.

## Task 1: Lock The Profile List Contract

**Files:**
- Modify: `tests/SalmonEgg.Presentation.Core.Tests/Settings/AcpConnectionSettingsXamlTests.cs`
- Read: `SalmonEgg/SalmonEgg/Presentation/Views/Settings/AcpConnectionSettingsPage.xaml`

- [ ] **Step 1: Add a focused contract test for profile list bindings and native actions**

Add this test method to `AcpConnectionSettingsXamlTests` after `AcpConnectionSettingsPage_ProfileCommandsStayInSectionHeader`:

```csharp
[Fact]
public void AcpConnectionSettingsPage_ProfileList_PreservesNativeSelectionAndActions()
{
    var xaml = LoadFile(@"SalmonEgg\SalmonEgg\Presentation\Views\Settings\AcpConnectionSettingsPage.xaml");

    Assert.Contains("<ListView ItemsSource=\"{x:Bind ViewModel.Profiles.ProfileItems, Mode=OneWay}\"", xaml, StringComparison.Ordinal);
    Assert.Contains("SelectedItem=\"{x:Bind ViewModel.Profiles.SelectedProfileItem, Mode=TwoWay}\"", xaml, StringComparison.Ordinal);
    Assert.Contains("SelectionMode=\"Single\"", xaml, StringComparison.Ordinal);
    Assert.Contains("<ToggleSwitch", xaml, StringComparison.Ordinal);
    Assert.Contains("IsOn=\"{x:Bind IsConnected, Mode=OneWay}\"", xaml, StringComparison.Ordinal);
    Assert.Contains("Toggled=\"OnProfileConnectionToggleToggled\"", xaml, StringComparison.Ordinal);
    Assert.Contains("<Button.Flyout>", xaml, StringComparison.Ordinal);
    Assert.Contains("<MenuFlyout>", xaml, StringComparison.Ordinal);
    Assert.Contains("Click=\"OnEditProfileMenuClick\"", xaml, StringComparison.Ordinal);
    Assert.Contains("Click=\"OnDeleteProfileMenuClick\"", xaml, StringComparison.Ordinal);
}
```

- [ ] **Step 2: Run the new test before implementation**

Run:

```powershell
dotnet test .\tests\SalmonEgg.Presentation.Core.Tests\SalmonEgg.Presentation.Core.Tests.csproj --filter "FullyQualifiedName~AcpConnectionSettingsXamlTests" --no-restore
```

Expected: PASS. This test protects existing behavior before visual edits.

- [ ] **Step 3: Commit the contract test**

Run:

```powershell
git add -- .\tests\SalmonEgg.Presentation.Core.Tests\Settings\AcpConnectionSettingsXamlTests.cs
git commit -m "test(settings): lock acp profile list contract"
```

Expected: commit succeeds.

## Task 2: Refine The Profile Row Layout

**Files:**
- Modify: `SalmonEgg/SalmonEgg/Presentation/Views/Settings/AcpConnectionSettingsPage.xaml`
- Test: `tests/SalmonEgg.Presentation.Core.Tests/Settings/AcpConnectionSettingsXamlTests.cs`

- [ ] **Step 1: Update the profile row XAML only**

In `AcpConnectionSettingsPage.xaml`, inside the profile `ListView.ItemTemplate`, replace the current root `Grid Padding="14,10"` block with this layout:

```xml
<Grid Padding="14,12"
      ColumnDefinitions="Auto,*,Auto"
      ColumnSpacing="14"
      RowDefinitions="Auto,Auto">
    <Border Grid.RowSpan="2"
            Width="36"
            Height="36"
            CornerRadius="8"
            Background="{ThemeResource AccentFillColorDefaultBrush}"
            Opacity="0.18"
            VerticalAlignment="Center">
        <Viewbox Width="18" Height="18" HorizontalAlignment="Center" VerticalAlignment="Center">
            <FontIcon FontFamily="{ThemeResource SymbolThemeFontFamily}"
                      Glyph="{x:Bind TransportGlyph}"
                      Foreground="{ThemeResource AccentBrush}"/>
        </Viewbox>
    </Border>

    <StackPanel Grid.Column="1"
                Spacing="3"
                VerticalAlignment="Center">
        <TextBlock Text="{x:Bind Name}"
                   FontWeight="SemiBold"
                   TextTrimming="CharacterEllipsis"/>
        <TextBlock Text="{x:Bind EndpointDescription}"
                   FontSize="12"
                   Opacity="0.65"
                   TextTrimming="CharacterEllipsis"/>
    </StackPanel>

    <Grid Grid.Column="2"
          MinWidth="188"
          ColumnDefinitions="Auto,Auto,Auto"
          ColumnSpacing="10"
          VerticalAlignment="Center"
          HorizontalAlignment="Right">
        <Grid VerticalAlignment="Center">
            <Border CornerRadius="12"
                    Padding="8,4"
                    Background="{ThemeResource SystemFillColorSuccessBackgroundBrush}"
                    Visibility="{x:Bind IsConnected, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}">
                <TextBlock Text="{x:Bind StatusLabel, Mode=OneWay}"
                           FontSize="12"
                           FontWeight="Medium"
                           Foreground="{ThemeResource SystemFillColorSuccessBrush}"/>
            </Border>

            <Border CornerRadius="12"
                    Padding="8,4"
                    Background="{ThemeResource SystemFillColorNeutralBrush}"
                    Visibility="{x:Bind IsNotConnected, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}">
                <TextBlock Text="{x:Bind StatusLabel, Mode=OneWay}"
                           FontSize="12"
                           FontWeight="Medium"
                           Foreground="{ThemeResource TextFillColorSecondaryBrush}"/>
            </Border>
        </Grid>

        <ToggleSwitch Grid.Column="1"
                      VerticalAlignment="Center"
                      OffContent=""
                      OnContent=""
                      IsOn="{x:Bind IsConnected, Mode=OneWay}"
                      IsEnabled="{x:Bind IsConnecting, Mode=OneWay, Converter={StaticResource InverseBooleanConverter}}"
                      Toggled="OnProfileConnectionToggleToggled"/>

        <Button Grid.Column="2"
                VerticalAlignment="Center"
                x:Uid="Acp_ProfileMore"
                ToolTipService.ToolTip="更多">
            <Button.Content>
                <FontIcon FontFamily="{ThemeResource SymbolThemeFontFamily}" Glyph="&#xE712;"/>
            </Button.Content>
            <Button.Flyout>
                <MenuFlyout>
                    <MenuFlyoutItem x:Uid="Acp_ProfileEdit"
                                    Text="编辑"
                                    Tag="{x:Bind ProfileId}"
                                    Click="OnEditProfileMenuClick"/>
                    <MenuFlyoutItem x:Uid="Acp_ProfileDelete"
                                    Text="删除"
                                    Tag="{x:Bind ProfileId}"
                                    Foreground="{ThemeResource SystemFillColorCriticalBrush}"
                                    Click="OnDeleteProfileMenuClick"/>
                </MenuFlyout>
            </Button.Flyout>
        </Button>
    </Grid>
</Grid>
```

Do not edit path mapping, hydration policy, code-behind, ViewModels, converters, or resources in this task.

- [ ] **Step 2: Run focused settings XAML tests**

Run:

```powershell
dotnet test .\tests\SalmonEgg.Presentation.Core.Tests\SalmonEgg.Presentation.Core.Tests.csproj --filter "FullyQualifiedName~AcpConnectionSettingsXamlTests" --no-restore
```

Expected: PASS.

- [ ] **Step 3: Commit the layout refinement**

Run:

```powershell
git add -- .\SalmonEgg\SalmonEgg\Presentation\Views\Settings\AcpConnectionSettingsPage.xaml
git commit -m "style(settings): refine acp profile rows"
```

Expected: commit succeeds.

## Task 3: Verify The App Build

**Files:**
- Read: `SalmonEgg/SalmonEgg/SalmonEgg.csproj`
- Read: `tests/SalmonEgg.Presentation.Core.Tests/Settings/AcpConnectionSettingsXamlTests.cs`

- [ ] **Step 1: Run the relevant Presentation Core settings tests**

Run:

```powershell
dotnet test .\tests\SalmonEgg.Presentation.Core.Tests\SalmonEgg.Presentation.Core.Tests.csproj --filter "FullyQualifiedName~Settings" --no-restore
```

Expected: PASS.

- [ ] **Step 2: Build the app project**

Run:

```powershell
dotnet build .\SalmonEgg\SalmonEgg\SalmonEgg.csproj --no-restore
```

Expected: build succeeds with 0 errors.

- [ ] **Step 3: Check final git status**

Run:

```powershell
git status --short --branch
```

Expected: clean working tree on `main`, ahead of origin by the new commits if they have not been pushed.

