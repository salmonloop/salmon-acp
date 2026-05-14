# Settings Center UX Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refactor the settings center page interiors into a calmer, more consistent WinUI-native settings experience while preserving the existing top category navigation.

**Architecture:** Keep `SettingsShellPage` as the top-navigation shell and limit implementation to XAML layout, shared style resources, and XAML contract tests. ViewModels remain the source of truth; code-behind stays limited to existing navigation, dialog, and command invocation paths.

**Tech Stack:** Uno Platform / WinUI 3 XAML, CommunityToolkit.Mvvm ViewModels, xUnit XAML compliance tests, `dotnet test`, `dotnet build`.

---

## File Structure

- Modify `SalmonEgg/SalmonEgg/App.xaml`: add shared settings typography, row, section, and command spacing styles/resources. Do not add new control templates.
- Modify `SalmonEgg/SalmonEgg/Presentation/Views/GeneralSettingsPage.xaml`: add page title/summary and settings rows for startup/window/language.
- Modify `SalmonEgg/SalmonEgg/Presentation/Views/Settings/AppearanceSettingsPage.xaml`: add page title/summary and settings rows for theme/motion/backdrop.
- Modify `SalmonEgg/SalmonEgg/Presentation/Views/Settings/AcpConnectionSettingsPage.xaml`: add page title/summary, section header command placement, profile rows, path mapping rows, and advanced hydration disclosure.
- Modify `SalmonEgg/SalmonEgg/Presentation/Views/Settings/DataStorageSettingsPage.xaml`: add page title/summary, row-based routine settings, command rows, and progressive disclosure for dangerous actions.
- Modify `SalmonEgg/SalmonEgg/Presentation/Views/Settings/ShortcutsSettingsPage.xaml`: add page title/summary and stable shortcut rows.
- Modify `SalmonEgg/SalmonEgg/Presentation/Views/Settings/DiagnosticsSettingsPage.xaml`: add page title/summary, key/value rows, command rows, and preserve live log `Expander`.
- Modify `SalmonEgg/SalmonEgg/Presentation/Views/Settings/AboutPage.xaml`: add page title/summary, key/value rows, command rows, and preserve acknowledgements `ListView`.
- Modify `tests/SalmonEgg.Presentation.Core.Tests/Ui/XamlComplianceTests.cs`: add contract tests for shared settings page structure and native behavior guardrails.
- Modify `tests/SalmonEgg.Presentation.Core.Tests/Settings/AcpConnectionSettingsXamlTests.cs`: keep path mapping tests and add hydration advanced disclosure coverage.
- Modify `tests/SalmonEgg.Presentation.Core.Tests/Settings/DiagnosticsSettingsPageXamlTests.cs`: keep unload cleanup coverage and add live log disclosure coverage if not already covered by `XamlComplianceTests`.

## Implementation Notes

- Keep `SettingsShellPage.xaml` top navigation unchanged except for spacing polish if absolutely necessary.
- Do not create custom controls for rows. Use shared styles and shallow `Grid` layouts.
- Do not use `{Binding}` where `{x:Bind}` already works.
- Do not add platform `#if` blocks for layout.
- Do not replace native control templates. Avoid expanding use of `SubtleButtonStyle`; prefer ordinary native `Button`, `AccentButtonStyle`, `HyperlinkButton`, or local lightweight resources only when a specific native resource needs tuning.
- Keep `x:Uid` on new user-visible labels and summaries. Add corresponding resource entries only if tests or runtime localization require them; otherwise match the existing XAML fallback pattern.

---

### Task 1: Add Shared Settings XAML Resources

**Files:**
- Modify: `SalmonEgg/SalmonEgg/App.xaml`
- Test: `tests/SalmonEgg.Presentation.Core.Tests/Ui/XamlComplianceTests.cs`

- [ ] **Step 1: Add failing resource contract test**

Append this test to `XamlComplianceTests`:

```csharp
[Fact]
public void AppResources_DefineNativeSettingsPageLayoutStyles()
{
    var xaml = LoadXaml(@"SalmonEgg\SalmonEgg\App.xaml");

    Assert.Contains("x:Key=\"SettingsPageTitleTextStyle\"", xaml, StringComparison.Ordinal);
    Assert.Contains("x:Key=\"SettingsPageSummaryTextStyle\"", xaml, StringComparison.Ordinal);
    Assert.Contains("x:Key=\"SettingsSectionTitleTextStyle\"", xaml, StringComparison.Ordinal);
    Assert.Contains("x:Key=\"SettingsRowTitleTextStyle\"", xaml, StringComparison.Ordinal);
    Assert.Contains("x:Key=\"SettingsRowDescriptionTextStyle\"", xaml, StringComparison.Ordinal);
    Assert.Contains("x:Key=\"SettingsSectionContainerStyle\"", xaml, StringComparison.Ordinal);
    Assert.Contains("x:Key=\"SettingsRowGridStyle\"", xaml, StringComparison.Ordinal);
    Assert.DoesNotContain("x:Key=\"SettingsRowControlTemplate\"", xaml, StringComparison.Ordinal);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test tests/SalmonEgg.Presentation.Core.Tests/SalmonEgg.Presentation.Core.Tests.csproj --filter "FullyQualifiedName~XamlComplianceTests.AppResources_DefineNativeSettingsPageLayoutStyles"
```

Expected: FAIL because the new settings styles do not exist yet.

- [ ] **Step 3: Add shared styles/resources**

In `App.xaml`, after `SettingsContentMaxWidth`, add:

```xml
<Thickness x:Key="SettingsPageVerticalPadding">0,0,0,32</Thickness>
<Thickness x:Key="SettingsSectionContainerPadding">0</Thickness>
<Thickness x:Key="SettingsRowPadding">16,14</Thickness>
<x:Double x:Key="SettingsRowControlMinWidth">220</x:Double>

<Style x:Key="SettingsPageTitleTextStyle" TargetType="TextBlock">
    <Setter Property="FontSize" Value="28" />
    <Setter Property="FontWeight" Value="SemiBold" />
    <Setter Property="Foreground" Value="{ThemeResource TextFillColorPrimaryBrush}" />
    <Setter Property="TextWrapping" Value="Wrap" />
</Style>

<Style x:Key="SettingsPageSummaryTextStyle" TargetType="TextBlock">
    <Setter Property="FontSize" Value="13" />
    <Setter Property="Foreground" Value="{ThemeResource TextFillColorSecondaryBrush}" />
    <Setter Property="TextWrapping" Value="Wrap" />
    <Setter Property="MaxWidth" Value="{StaticResource SettingsContentMaxWidth}" />
</Style>

<Style x:Key="SettingsSectionTitleTextStyle" TargetType="TextBlock">
    <Setter Property="FontSize" Value="14" />
    <Setter Property="FontWeight" Value="SemiBold" />
    <Setter Property="Foreground" Value="{ThemeResource TextFillColorPrimaryBrush}" />
    <Setter Property="TextWrapping" Value="Wrap" />
</Style>

<Style x:Key="SettingsRowTitleTextStyle" TargetType="TextBlock">
    <Setter Property="FontSize" Value="14" />
    <Setter Property="FontWeight" Value="SemiBold" />
    <Setter Property="Foreground" Value="{ThemeResource TextFillColorPrimaryBrush}" />
    <Setter Property="TextWrapping" Value="Wrap" />
</Style>

<Style x:Key="SettingsRowDescriptionTextStyle" TargetType="TextBlock">
    <Setter Property="FontSize" Value="12" />
    <Setter Property="Foreground" Value="{ThemeResource TextFillColorSecondaryBrush}" />
    <Setter Property="TextWrapping" Value="Wrap" />
</Style>

<Style x:Key="SettingsSectionContainerStyle" TargetType="Border">
    <Setter Property="Background" Value="{ThemeResource LayerOnAcrylicFillColorDefaultBrush}" />
    <Setter Property="BorderBrush" Value="{ThemeResource DividerStrokeColorDefaultBrush}" />
    <Setter Property="BorderThickness" Value="1" />
    <Setter Property="CornerRadius" Value="8" />
    <Setter Property="Padding" Value="{StaticResource SettingsSectionContainerPadding}" />
</Style>

<Style x:Key="SettingsRowGridStyle" TargetType="Grid">
    <Setter Property="Padding" Value="{StaticResource SettingsRowPadding}" />
    <Setter Property="ColumnSpacing" Value="16" />
    <Setter Property="RowSpacing" Value="4" />
</Style>
```

- [ ] **Step 4: Run test to verify it passes**

Run:

```powershell
dotnet test tests/SalmonEgg.Presentation.Core.Tests/SalmonEgg.Presentation.Core.Tests.csproj --filter "FullyQualifiedName~XamlComplianceTests.AppResources_DefineNativeSettingsPageLayoutStyles"
```

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add SalmonEgg/SalmonEgg/App.xaml tests/SalmonEgg.Presentation.Core.Tests/Ui/XamlComplianceTests.cs
git commit -m "style(settings): add shared settings layout resources"
```

---

### Task 2: Add Page-Level Structure Contracts

**Files:**
- Modify: `tests/SalmonEgg.Presentation.Core.Tests/Ui/XamlComplianceTests.cs`

- [ ] **Step 1: Add failing page title/summary contract test**

Append this test to `XamlComplianceTests`:

```csharp
[Fact]
public void SettingsSubPages_ExposePageTitlesAndSummaries()
{
    string[] pages =
    [
        @"SalmonEgg\SalmonEgg\Presentation\Views\GeneralSettingsPage.xaml",
        @"SalmonEgg\SalmonEgg\Presentation\Views\Settings\AppearanceSettingsPage.xaml",
        @"SalmonEgg\SalmonEgg\Presentation\Views\Settings\AcpConnectionSettingsPage.xaml",
        @"SalmonEgg\SalmonEgg\Presentation\Views\Settings\DataStorageSettingsPage.xaml",
        @"SalmonEgg\SalmonEgg\Presentation\Views\Settings\ShortcutsSettingsPage.xaml",
        @"SalmonEgg\SalmonEgg\Presentation\Views\Settings\DiagnosticsSettingsPage.xaml",
        @"SalmonEgg\SalmonEgg\Presentation\Views\Settings\AboutPage.xaml"
    ];

    foreach (var page in pages)
    {
        var xaml = LoadXaml(page);

        Assert.Contains("Style=\"{StaticResource SettingsPageTitleTextStyle}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SettingsPageSummaryTextStyle}\"", xaml, StringComparison.Ordinal);
    }
}
```

- [ ] **Step 2: Add failing native-shell preservation test**

Keep or update the existing `SettingsShell_KeepsSectionNavigationAtTheTop` test so it contains:

```csharp
[Fact]
public void SettingsShell_KeepsSectionNavigationAtTheTop()
{
    var xaml = LoadXaml(@"SalmonEgg\SalmonEgg\Presentation\Views\SettingsShellPage.xaml");

    Assert.Contains("<Setter Property=\"PaneDisplayMode\" Value=\"Top\" />", xaml);
    Assert.DoesNotContain("PaneDisplayMode\" Value=\"Left", xaml, StringComparison.Ordinal);
    Assert.DoesNotContain("<NavigationViewItemHeader", xaml, StringComparison.Ordinal);
}
```

- [ ] **Step 3: Run tests to verify the new page contract fails**

Run:

```powershell
dotnet test tests/SalmonEgg.Presentation.Core.Tests/SalmonEgg.Presentation.Core.Tests.csproj --filter "FullyQualifiedName~XamlComplianceTests.SettingsSubPages_ExposePageTitlesAndSummaries|FullyQualifiedName~XamlComplianceTests.SettingsShell_KeepsSectionNavigationAtTheTop"
```

Expected: `SettingsSubPages_ExposePageTitlesAndSummaries` FAILS until pages are updated. `SettingsShell_KeepsSectionNavigationAtTheTop` PASSES.

- [ ] **Step 4: Commit**

```powershell
git add tests/SalmonEgg.Presentation.Core.Tests/Ui/XamlComplianceTests.cs
git commit -m "test(settings): require page titles and summaries"
```

---

### Task 3: Refactor General And Appearance Pages

**Files:**
- Modify: `SalmonEgg/SalmonEgg/Presentation/Views/GeneralSettingsPage.xaml`
- Modify: `SalmonEgg/SalmonEgg/Presentation/Views/Settings/AppearanceSettingsPage.xaml`
- Test: `tests/SalmonEgg.Presentation.Core.Tests/Ui/XamlComplianceTests.cs`

- [ ] **Step 1: Add focused structure assertions**

Append this test to `XamlComplianceTests`:

```csharp
[Fact]
public void GeneralAndAppearanceSettingsPages_UseNativeSettingsRows()
{
    var general = LoadXaml(@"SalmonEgg\SalmonEgg\Presentation\Views\GeneralSettingsPage.xaml");
    var appearance = LoadXaml(@"SalmonEgg\SalmonEgg\Presentation\Views\Settings\AppearanceSettingsPage.xaml");

    Assert.Contains("x:Uid=\"General_PageTitle\"", general, StringComparison.Ordinal);
    Assert.Contains("x:Uid=\"General_PageSummary\"", general, StringComparison.Ordinal);
    Assert.Contains("Style=\"{StaticResource SettingsRowGridStyle}\"", general, StringComparison.Ordinal);
    Assert.Contains("<ToggleSwitch", general, StringComparison.Ordinal);
    Assert.Contains("<ComboBox", general, StringComparison.Ordinal);

    Assert.Contains("x:Uid=\"Appearance_PageTitle\"", appearance, StringComparison.Ordinal);
    Assert.Contains("x:Uid=\"Appearance_PageSummary\"", appearance, StringComparison.Ordinal);
    Assert.Contains("Style=\"{StaticResource SettingsRowGridStyle}\"", appearance, StringComparison.Ordinal);
    Assert.Contains("<ToggleSwitch", appearance, StringComparison.Ordinal);
    Assert.Contains("<ComboBox", appearance, StringComparison.Ordinal);
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run:

```powershell
dotnet test tests/SalmonEgg.Presentation.Core.Tests/SalmonEgg.Presentation.Core.Tests.csproj --filter "FullyQualifiedName~XamlComplianceTests.GeneralAndAppearanceSettingsPages_UseNativeSettingsRows|FullyQualifiedName~XamlComplianceTests.SettingsSubPages_ExposePageTitlesAndSummaries"
```

Expected: FAIL because page title/summary and row styles are not yet present on these pages.

- [ ] **Step 3: Update `GeneralSettingsPage.xaml`**

Keep the existing root, resources, bindings, and `ResponsiveSettingsHost`. Replace the inner `StackPanel` content with this structure:

```xml
<StackPanel Spacing="24" Padding="{StaticResource SettingsPageVerticalPadding}">
    <StackPanel Spacing="4">
        <TextBlock x:Uid="General_PageTitle"
                   Text="常规"
                   Style="{StaticResource SettingsPageTitleTextStyle}" />
        <TextBlock x:Uid="General_PageSummary"
                   Text="管理启动、窗口行为和界面语言。"
                   Style="{StaticResource SettingsPageSummaryTextStyle}" />
    </StackPanel>

    <StackPanel Spacing="8">
        <TextBlock x:Uid="General_StartupTitle"
                   Text="启动与窗口"
                   Style="{StaticResource SettingsSectionTitleTextStyle}" />
        <Border Style="{StaticResource SettingsSectionContainerStyle}">
            <StackPanel>
                <Grid Style="{StaticResource SettingsRowGridStyle}"
                      ColumnDefinitions="*,Auto">
                    <StackPanel Spacing="3">
                        <TextBlock x:Uid="General_AutoStart"
                                   Text="开机自动启动"
                                   Style="{StaticResource SettingsRowTitleTextStyle}" />
                        <TextBlock x:Uid="General_AutoStartUnsupported"
                                   Text="当前平台不支持开机自动启动"
                                   Style="{StaticResource SettingsRowDescriptionTextStyle}"
                                   Visibility="{x:Bind ViewModel.Preferences.IsLaunchOnStartupSupported, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}, ConverterParameter=Invert}" />
                    </StackPanel>
                    <ToggleSwitch Grid.Column="1"
                                  IsOn="{x:Bind ViewModel.Preferences.LaunchOnStartup, Mode=TwoWay}"
                                  IsEnabled="{x:Bind ViewModel.Preferences.IsLaunchOnStartupSupported, Mode=OneWay}"
                                  OnContent="开启"
                                  OffContent="关闭" />
                </Grid>

                <MenuFlyoutSeparator Margin="16,0" />

                <Grid Style="{StaticResource SettingsRowGridStyle}"
                      ColumnDefinitions="*,Auto">
                    <StackPanel Spacing="3">
                        <TextBlock x:Uid="General_MinimizeToTray"
                                   Text="最小化到系统托盘"
                                   Style="{StaticResource SettingsRowTitleTextStyle}" />
                        <TextBlock x:Uid="General_TrayUnsupported"
                                   Text="当前平台暂未集成系统托盘支持"
                                   Style="{StaticResource SettingsRowDescriptionTextStyle}"
                                   Visibility="{x:Bind ViewModel.Preferences.IsMinimizeToTraySupported, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}, ConverterParameter=Invert}" />
                    </StackPanel>
                    <ToggleSwitch Grid.Column="1"
                                  IsOn="{x:Bind ViewModel.Preferences.MinimizeToTray, Mode=TwoWay}"
                                  IsEnabled="{x:Bind ViewModel.Preferences.IsMinimizeToTraySupported, Mode=OneWay}"
                                  OnContent="开启"
                                  OffContent="关闭" />
                </Grid>
            </StackPanel>
        </Border>
    </StackPanel>

    <StackPanel Spacing="8">
        <TextBlock x:Uid="General_LanguageTitle"
                   Text="语言与地区"
                   Style="{StaticResource SettingsSectionTitleTextStyle}" />
        <Border Style="{StaticResource SettingsSectionContainerStyle}">
            <Grid Style="{StaticResource SettingsRowGridStyle}"
                  ColumnDefinitions="*,Auto">
                <StackPanel Spacing="3">
                    <TextBlock x:Uid="General_LanguageLabel"
                               Text="选择界面语言"
                               Style="{StaticResource SettingsRowTitleTextStyle}" />
                    <TextBlock x:Uid="General_LanguageRestartHint"
                               Text="部分设置在重新启动应用后生效"
                               Style="{StaticResource SettingsRowDescriptionTextStyle}" />
                    <TextBlock x:Uid="General_LanguageUnsupported"
                               Text="当前平台不支持语言覆盖"
                               Style="{StaticResource SettingsRowDescriptionTextStyle}"
                               Visibility="{x:Bind ViewModel.Preferences.IsLanguageOverrideSupported, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}, ConverterParameter=Invert}" />
                </StackPanel>
                <ComboBox Grid.Column="1"
                          ItemsSource="{x:Bind ViewModel.Preferences.LanguageOptions, Mode=OneWay}"
                          SelectedValue="{x:Bind ViewModel.Preferences.Language, Mode=TwoWay}"
                          SelectedValuePath="Tag"
                          MinWidth="{StaticResource SettingsRowControlMinWidth}"
                          IsEnabled="{x:Bind ViewModel.Preferences.IsLanguageOverrideSupported, Mode=OneWay}">
                    <ComboBox.ItemTemplate>
                        <DataTemplate x:DataType="settings:AppLanguageOptionViewModel">
                            <TextBlock Text="{x:Bind DisplayNameResourceKey, Mode=OneWay, Converter={StaticResource ResourceStringConverter}}" />
                        </DataTemplate>
                    </ComboBox.ItemTemplate>
                </ComboBox>
            </Grid>
        </Border>
    </StackPanel>
</StackPanel>
```

- [ ] **Step 4: Update `AppearanceSettingsPage.xaml`**

Keep the existing root, `Preferences` bindings, and `ResponsiveSettingsHost`. Replace the inner content with:

```xml
<StackPanel Spacing="24" Padding="{StaticResource SettingsPageVerticalPadding}">
    <StackPanel Spacing="4">
        <TextBlock x:Uid="Appearance_PageTitle"
                   Text="外观"
                   Style="{StaticResource SettingsPageTitleTextStyle}" />
        <TextBlock x:Uid="Appearance_PageSummary"
                   Text="调整主题、过渡动画和窗口背景材质。"
                   Style="{StaticResource SettingsPageSummaryTextStyle}" />
    </StackPanel>

    <InfoBar x:Uid="Appearance_Notice"
             Title="提示"
             Message="主题、动画和材质设置会立即生效（部分平台可能需要重启）。"
             IsOpen="True"
             IsClosable="False"
             Severity="Informational" />

    <StackPanel Spacing="8">
        <TextBlock x:Uid="Appearance_ThemeTitle"
                   Text="界面主题"
                   Style="{StaticResource SettingsSectionTitleTextStyle}" />
        <Border Style="{StaticResource SettingsSectionContainerStyle}">
            <Grid Style="{StaticResource SettingsRowGridStyle}"
                  ColumnDefinitions="*,Auto">
                <StackPanel Spacing="3">
                    <TextBlock x:Uid="Appearance_ThemeDescription"
                               Text="应用主题"
                               Style="{StaticResource SettingsRowTitleTextStyle}" />
                    <TextBlock x:Uid="Appearance_ThemeSummary"
                               Text="选择您偏好的应用主题颜色。"
                               Style="{StaticResource SettingsRowDescriptionTextStyle}" />
                </StackPanel>
                <ComboBox Grid.Column="1"
                          SelectedValue="{x:Bind Preferences.Theme, Mode=TwoWay}"
                          SelectedValuePath="Tag"
                          MinWidth="{StaticResource SettingsRowControlMinWidth}">
                    <ComboBoxItem x:Uid="Appearance_ThemeSystem" Content="跟随系统" Tag="System" />
                    <ComboBoxItem x:Uid="Appearance_ThemeLight" Content="浅色" Tag="Light" />
                    <ComboBoxItem x:Uid="Appearance_ThemeDark" Content="深色" Tag="Dark" />
                </ComboBox>
            </Grid>
        </Border>
    </StackPanel>

    <StackPanel Spacing="8">
        <TextBlock x:Uid="Appearance_MotionTitle"
                   Text="交互体验"
                   Style="{StaticResource SettingsSectionTitleTextStyle}" />
        <Border Style="{StaticResource SettingsSectionContainerStyle}">
            <Grid Style="{StaticResource SettingsRowGridStyle}"
                  ColumnDefinitions="*,Auto">
                <StackPanel Spacing="3">
                    <TextBlock x:Uid="Appearance_MotionToggleTitle"
                               Text="全局过渡动画"
                               Style="{StaticResource SettingsRowTitleTextStyle}" />
                    <TextBlock x:Uid="Appearance_MotionToggleDescription"
                               Text="开启后，页面切换将使用平滑过渡效果。"
                               Style="{StaticResource SettingsRowDescriptionTextStyle}" />
                </StackPanel>
                <ToggleSwitch Grid.Column="1"
                              IsOn="{x:Bind Preferences.IsAnimationEnabled, Mode=TwoWay}"
                              x:Uid="Appearance_MotionToggle"
                              OnContent="已开启"
                              OffContent="已禁用"
                              MinWidth="0"
                              VerticalAlignment="Center" />
            </Grid>
        </Border>
    </StackPanel>

    <StackPanel Spacing="8">
        <TextBlock x:Uid="Appearance_BackdropTitle"
                   Text="背景材质"
                   Style="{StaticResource SettingsSectionTitleTextStyle}" />
        <Border Style="{StaticResource SettingsSectionContainerStyle}">
            <Grid Style="{StaticResource SettingsRowGridStyle}"
                  ColumnDefinitions="*,Auto">
                <StackPanel Spacing="3">
                    <TextBlock x:Uid="Appearance_BackdropDescription"
                               Text="窗口背景效果"
                               Style="{StaticResource SettingsRowTitleTextStyle}" />
                    <TextBlock x:Uid="Appearance_BackdropSummary"
                               Text="不同系统版本会自动降级。"
                               Style="{StaticResource SettingsRowDescriptionTextStyle}" />
                </StackPanel>
                <ComboBox Grid.Column="1"
                          SelectedValue="{x:Bind Preferences.Backdrop, Mode=TwoWay}"
                          SelectedValuePath="Tag"
                          MinWidth="{StaticResource SettingsRowControlMinWidth}">
                    <ComboBoxItem x:Uid="Appearance_BackdropSystem" Content="自动（系统推荐）" Tag="System" />
                    <ComboBoxItem Content="Mica" Tag="Mica" />
                    <ComboBoxItem Content="Acrylic" Tag="Acrylic" />
                    <ComboBoxItem x:Uid="Appearance_BackdropSolid" Content="纯色" Tag="Solid" />
                </ComboBox>
            </Grid>
        </Border>
    </StackPanel>
</StackPanel>
```

- [ ] **Step 5: Run tests**

Run:

```powershell
dotnet test tests/SalmonEgg.Presentation.Core.Tests/SalmonEgg.Presentation.Core.Tests.csproj --filter "FullyQualifiedName~XamlComplianceTests.GeneralAndAppearanceSettingsPages_UseNativeSettingsRows|FullyQualifiedName~XamlComplianceTests.SettingsSubPages_ExposePageTitlesAndSummaries|FullyQualifiedName~XamlComplianceTests.AppearanceSettingsPage_MotionPreferenceIsActionable|FullyQualifiedName~XamlComplianceTests.GeneralSettingsPage_DoesNotDuplicateCacheMaintenance"
```

Expected: General/Appearance-specific tests PASS. The all-subpages title/summary test still FAILS until later tasks update every settings subpage.

- [ ] **Step 6: Commit**

```powershell
git add SalmonEgg/SalmonEgg/Presentation/Views/GeneralSettingsPage.xaml SalmonEgg/SalmonEgg/Presentation/Views/Settings/AppearanceSettingsPage.xaml tests/SalmonEgg.Presentation.Core.Tests/Ui/XamlComplianceTests.cs
git commit -m "style(settings): refine general and appearance layouts"
```

---

### Task 4: Refactor ACP / Agent Page

**Files:**
- Modify: `SalmonEgg/SalmonEgg/Presentation/Views/Settings/AcpConnectionSettingsPage.xaml`
- Modify: `tests/SalmonEgg.Presentation.Core.Tests/Settings/AcpConnectionSettingsXamlTests.cs`

- [ ] **Step 1: Add failing ACP structure tests**

Append these tests to `AcpConnectionSettingsXamlTests`:

```csharp
[Fact]
public void AcpConnectionSettingsPage_ExposesPageTitleSummaryAndAdvancedHydrationDisclosure()
{
    var xaml = LoadFile(@"SalmonEgg\SalmonEgg\Presentation\Views\Settings\AcpConnectionSettingsPage.xaml");

    Assert.Contains("x:Uid=\"Acp_PageTitle\"", xaml, StringComparison.Ordinal);
    Assert.Contains("x:Uid=\"Acp_PageSummary\"", xaml, StringComparison.Ordinal);
    Assert.Contains("Style=\"{StaticResource SettingsPageTitleTextStyle}\"", xaml, StringComparison.Ordinal);
    Assert.Contains("Style=\"{StaticResource SettingsPageSummaryTextStyle}\"", xaml, StringComparison.Ordinal);
    Assert.Contains("<Expander", xaml, StringComparison.Ordinal);
    Assert.Contains("x:Uid=\"Acp_AdvancedExpander\"", xaml, StringComparison.Ordinal);
}

[Fact]
public void AcpConnectionSettingsPage_ProfileCommandsStayInSectionHeader()
{
    var xaml = LoadFile(@"SalmonEgg\SalmonEgg\Presentation\Views\Settings\AcpConnectionSettingsPage.xaml");

    Assert.Contains("x:Uid=\"Acp_ProfilesTitle\"", xaml, StringComparison.Ordinal);
    Assert.Contains("Command=\"{x:Bind ViewModel.Profiles.RefreshCommand}\"", xaml, StringComparison.Ordinal);
    Assert.Contains("Click=\"OnAddProfileClick\"", xaml, StringComparison.Ordinal);
    Assert.Contains("ItemsSource=\"{x:Bind ViewModel.Profiles.ProfileItems, Mode=OneWay}\"", xaml, StringComparison.Ordinal);
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run:

```powershell
dotnet test tests/SalmonEgg.Presentation.Core.Tests/SalmonEgg.Presentation.Core.Tests.csproj --filter "FullyQualifiedName~AcpConnectionSettingsXamlTests"
```

Expected: new tests FAIL, existing path mapping tests PASS.

- [ ] **Step 3: Update top-level page content**

In `AcpConnectionSettingsPage.xaml`, replace the outer content `StackPanel Spacing="28"` with:

```xml
<StackPanel Spacing="24" Padding="{StaticResource SettingsPageVerticalPadding}">
    <StackPanel Spacing="4">
        <TextBlock x:Uid="Acp_PageTitle"
                   Text="ACP / Agent"
                   Style="{StaticResource SettingsPageTitleTextStyle}" />
        <TextBlock x:Uid="Acp_PageSummary"
                   Text="管理 ACP 连接配置、本地路径映射和远程会话加载策略。"
                   Style="{StaticResource SettingsPageSummaryTextStyle}" />
    </StackPanel>

    <!-- Existing sections are moved here and rewritten by the next steps. -->
</StackPanel>
```

- [ ] **Step 4: Rewrite profile section with header commands and native ListView**

Use this section inside the page stack:

```xml
<StackPanel Spacing="8">
    <Grid ColumnDefinitions="*,Auto" ColumnSpacing="12">
        <TextBlock x:Uid="Acp_ProfilesTitle"
                   Text="ACP 连接配置"
                   Style="{StaticResource SettingsSectionTitleTextStyle}"
                   VerticalAlignment="Center" />
        <StackPanel Grid.Column="1"
                    Orientation="Horizontal"
                    Spacing="8"
                    VerticalAlignment="Center">
            <Button x:Uid="Acp_ProfilesRefresh"
                    Content="刷新"
                    Command="{x:Bind ViewModel.Profiles.RefreshCommand}"
                    IsEnabled="{x:Bind ViewModel.Profiles.IsLoading, Mode=OneWay, Converter={StaticResource InverseBooleanConverter}}" />
            <Button x:Uid="Acp_ProfilesAdd"
                    Content="新建配置"
                    Style="{StaticResource AccentButtonStyle}"
                    Click="OnAddProfileClick" />
        </StackPanel>
    </Grid>

    <Border Style="{StaticResource SettingsSectionContainerStyle}">
        <ListView ItemsSource="{x:Bind ViewModel.Profiles.ProfileItems, Mode=OneWay}"
                  SelectedItem="{x:Bind ViewModel.Profiles.SelectedProfileItem, Mode=TwoWay}"
                  SelectionMode="Single"
                  MinHeight="140"
                  Background="Transparent"
                  ItemContainerStyle="{StaticResource AgentListItemStyle}">
            <ListView.ItemTemplate>
                <DataTemplate x:DataType="vm:AgentProfileItemViewModel">
                    <Grid Style="{StaticResource SettingsRowGridStyle}"
                          ColumnDefinitions="Auto,*,Auto,Auto,Auto">
                        <Border Width="36"
                                Height="36"
                                CornerRadius="8"
                                Background="{ThemeResource AccentFillColorDefaultBrush}"
                                Opacity="0.18"
                                VerticalAlignment="Center">
                            <Viewbox Width="18" Height="18" HorizontalAlignment="Center" VerticalAlignment="Center">
                                <FontIcon FontFamily="{ThemeResource SymbolThemeFontFamily}"
                                          Glyph="{x:Bind TransportGlyph}"
                                          Foreground="{ThemeResource AccentBrush}" />
                            </Viewbox>
                        </Border>

                        <StackPanel Grid.Column="1" Spacing="2" VerticalAlignment="Center">
                            <TextBlock Text="{x:Bind Name}"
                                       Style="{StaticResource SettingsRowTitleTextStyle}"
                                       TextTrimming="CharacterEllipsis" />
                            <TextBlock Text="{x:Bind EndpointDescription}"
                                       Style="{StaticResource SettingsRowDescriptionTextStyle}"
                                       TextTrimming="CharacterEllipsis" />
                        </StackPanel>

                        <Grid Grid.Column="2" VerticalAlignment="Center">
                            <Border CornerRadius="12"
                                    Padding="8,4"
                                    Background="{ThemeResource SystemFillColorSuccessBackgroundBrush}"
                                    Visibility="{x:Bind IsConnected, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}">
                                <TextBlock Text="{x:Bind StatusLabel, Mode=OneWay}"
                                           FontSize="12"
                                           FontWeight="Medium"
                                           Foreground="{ThemeResource SystemFillColorSuccessBrush}" />
                            </Border>
                            <Border CornerRadius="12"
                                    Padding="8,4"
                                    Background="{ThemeResource SystemFillColorNeutralBrush}"
                                    Visibility="{x:Bind IsNotConnected, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}">
                                <TextBlock Text="{x:Bind StatusLabel, Mode=OneWay}"
                                           FontSize="12"
                                           FontWeight="Medium"
                                           Foreground="{ThemeResource TextFillColorSecondaryBrush}" />
                            </Border>
                        </Grid>

                        <ToggleSwitch Grid.Column="3"
                                      VerticalAlignment="Center"
                                      OffContent=""
                                      OnContent=""
                                      IsOn="{x:Bind IsConnected, Mode=OneWay}"
                                      IsEnabled="{x:Bind IsConnecting, Mode=OneWay, Converter={StaticResource InverseBooleanConverter}}"
                                      Toggled="OnProfileConnectionToggleToggled" />

                        <Button Grid.Column="4"
                                VerticalAlignment="Center"
                                x:Uid="Acp_ProfileMore"
                                ToolTipService.ToolTip="更多">
                            <Button.Content>
                                <FontIcon FontFamily="{ThemeResource SymbolThemeFontFamily}" Glyph="&#xE712;" />
                            </Button.Content>
                            <Button.Flyout>
                                <MenuFlyout>
                                    <MenuFlyoutItem x:Uid="Acp_ProfileEdit"
                                                    Text="编辑"
                                                    Tag="{x:Bind ProfileId}"
                                                    Click="OnEditProfileMenuClick" />
                                    <MenuFlyoutItem x:Uid="Acp_ProfileDelete"
                                                    Text="删除"
                                                    Tag="{x:Bind ProfileId}"
                                                    Foreground="{ThemeResource SystemFillColorCriticalBrush}"
                                                    Click="OnDeleteProfileMenuClick" />
                                </MenuFlyout>
                            </Button.Flyout>
                        </Button>
                    </Grid>
                </DataTemplate>
            </ListView.ItemTemplate>
        </ListView>
    </Border>
</StackPanel>
```

- [ ] **Step 5: Rewrite path mappings section as row-based list**

Keep existing automation IDs and ViewModel bindings. Use:

```xml
<StackPanel Spacing="8"
            AutomationProperties.AutomationId="Acp.PathMappings.Section">
    <Grid ColumnDefinitions="*,Auto" ColumnSpacing="12">
        <StackPanel Spacing="3">
            <TextBlock x:Uid="Acp_PathMappingsTitle"
                       Text="本地路径映射"
                       Style="{StaticResource SettingsSectionTitleTextStyle}" />
            <TextBlock x:Uid="Acp_PathMappingsHint"
                       Text="SalmonEgg 使用这些映射把 Agent 返回的远程工作区路径转换为本地文件系统路径。"
                       Style="{StaticResource SettingsRowDescriptionTextStyle}" />
        </StackPanel>
        <Button Grid.Column="1"
                x:Uid="Acp_PathMappingsAdd"
                Content="新增映射"
                Command="{x:Bind ViewModel.AddPathMappingCommand}"
                AutomationProperties.AutomationId="Acp.PathMappings.Add" />
    </Grid>

    <Border Style="{StaticResource SettingsSectionContainerStyle}">
        <ListView ItemsSource="{x:Bind ViewModel.PathMappingRows, Mode=OneWay}"
                  SelectionMode="None"
                  MinHeight="120"
                  Background="Transparent"
                  ItemContainerStyle="{StaticResource AgentListItemStyle}"
                  AutomationProperties.AutomationId="Acp.PathMappings.List">
            <ListView.ItemTemplate>
                <DataTemplate x:DataType="vm:AcpPathMappingRowViewModel">
                    <Grid Style="{StaticResource SettingsRowGridStyle}"
                          ColumnDefinitions="*,Auto"
                          RowDefinitions="Auto,Auto">
                        <TextBox Grid.Row="0"
                                 Grid.Column="0"
                                 x:Uid="Acp_PathMappingsRemotePath"
                                 Header="远程根路径"
                                 PlaceholderText="/workspace/project"
                                 Text="{x:Bind RemoteRootPath, Mode=TwoWay}" />
                        <TextBox Grid.Row="1"
                                 Grid.Column="0"
                                 x:Uid="Acp_PathMappingsLocalPath"
                                 Header="本地根路径"
                                 PlaceholderText="C:\project 或 /Users/name/project"
                                 Text="{x:Bind LocalRootPath, Mode=TwoWay}" />
                        <Button Grid.Row="0"
                                Grid.RowSpan="2"
                                Grid.Column="1"
                                x:Uid="Acp_PathMappingsRemove"
                                Command="{x:Bind RemoveCommand}"
                                VerticalAlignment="Center">
                            <Button.Content>
                                <FontIcon FontFamily="{ThemeResource SymbolThemeFontFamily}"
                                          Glyph="&#xE74D;" />
                            </Button.Content>
                        </Button>
                    </Grid>
                </DataTemplate>
            </ListView.ItemTemplate>
        </ListView>
    </Border>
</StackPanel>
```

- [ ] **Step 6: Move hydration policy into advanced Expander**

Use:

```xml
<Expander x:Uid="Acp_AdvancedExpander"
          Header="高级 ACP 行为"
          HorizontalAlignment="Stretch"
          HorizontalContentAlignment="Stretch">
    <StackPanel Spacing="8" Padding="0,8,0,0">
        <TextBlock x:Uid="Acp_HydrationCompletionTitle"
                   Text="会话加载完成判定"
                   Style="{StaticResource SettingsSectionTitleTextStyle}" />
        <Border Style="{StaticResource SettingsSectionContainerStyle}">
            <Grid Style="{StaticResource SettingsRowGridStyle}"
                  ColumnDefinitions="*,Auto">
                <StackPanel Spacing="3">
                    <TextBlock x:Uid="Acp_HydrationCompletionHint"
                               Text="SalmonEgg 使用此设置决定远程 ACP 会话在 session/load 返回后何时可视为“已就绪”。"
                               Style="{StaticResource SettingsRowDescriptionTextStyle}" />
                    <TextBlock Text="{x:Bind ViewModel.SelectedHydrationCompletionMode, Mode=OneWay, Converter={StaticResource HydrationCompletionModeLocalizationConverter}, ConverterParameter=Description}"
                               Style="{StaticResource SettingsRowDescriptionTextStyle}" />
                </StackPanel>
                <ComboBox Grid.Column="1"
                          ItemsSource="{x:Bind ViewModel.HydrationCompletionModeOptions, Mode=OneWay}"
                          SelectedItem="{x:Bind ViewModel.SelectedHydrationCompletionMode, Mode=TwoWay}"
                          MinWidth="{StaticResource SettingsRowControlMinWidth}">
                    <ComboBox.ItemTemplate>
                        <DataTemplate x:DataType="vm:HydrationCompletionModeOptionViewModel">
                            <TextBlock Text="{x:Bind Value, Converter={StaticResource HydrationCompletionModeLocalizationConverter}}" />
                        </DataTemplate>
                    </ComboBox.ItemTemplate>
                </ComboBox>
            </Grid>
        </Border>
    </StackPanel>
</Expander>
```

- [ ] **Step 7: Run tests**

Run:

```powershell
dotnet test tests/SalmonEgg.Presentation.Core.Tests/SalmonEgg.Presentation.Core.Tests.csproj --filter "FullyQualifiedName~AcpConnectionSettingsXamlTests|FullyQualifiedName~XamlComplianceTests.SettingsSubPages_ExposePageTitlesAndSummaries"
```

Expected: ACP tests PASS. The all-subpages title/summary test still FAILS until remaining pages are updated.

- [ ] **Step 8: Commit**

```powershell
git add SalmonEgg/SalmonEgg/Presentation/Views/Settings/AcpConnectionSettingsPage.xaml tests/SalmonEgg.Presentation.Core.Tests/Settings/AcpConnectionSettingsXamlTests.cs
git commit -m "style(settings): refine acp agent layout"
```

---

### Task 5: Refactor Data And Storage Page

**Files:**
- Modify: `SalmonEgg/SalmonEgg/Presentation/Views/Settings/DataStorageSettingsPage.xaml`
- Modify: `tests/SalmonEgg.Presentation.Core.Tests/Ui/XamlComplianceTests.cs`

- [ ] **Step 1: Add failing data/storage disclosure test**

Update `DataStorageSettingsPage_SeparatesRoutineStorageAndDangerActions` to:

```csharp
[Fact]
public void DataStorageSettingsPage_SeparatesRoutineStorageAndDangerActions()
{
    var document = XDocument.Parse(LoadXaml(@"SalmonEgg\SalmonEgg\Presentation\Views\Settings\DataStorageSettingsPage.xaml"));
    var xaml = document.ToString(SaveOptions.DisableFormatting);
    var resetDefaults = FindElementByUid(document, "DataStorage_ResetDefaults");
    var clearAllData = FindElementByUid(document, "DataStorage_ClearAllData");

    Assert.Contains("x:Uid=\"DataStorage_PageTitle\"", xaml, StringComparison.Ordinal);
    Assert.Contains("x:Uid=\"DataStorage_PageSummary\"", xaml, StringComparison.Ordinal);
    Assert.Contains("<Expander", xaml, StringComparison.Ordinal);
    Assert.Contains("DataStorage_DangerTitle", xaml, StringComparison.Ordinal);
    Assert.Contains("DataStorage_DangerWarning", xaml, StringComparison.Ordinal);
    Assert.NotSame(resetDefaults.Parent, clearAllData.Parent);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test tests/SalmonEgg.Presentation.Core.Tests/SalmonEgg.Presentation.Core.Tests.csproj --filter "FullyQualifiedName~XamlComplianceTests.DataStorageSettingsPage_SeparatesRoutineStorageAndDangerActions"
```

Expected: FAIL because the page lacks title/summary and danger `Expander`.

- [ ] **Step 3: Update page title and routine sections**

In `DataStorageSettingsPage.xaml`, replace the inner content with a page stack using these patterns:

```xml
<StackPanel Spacing="24" Padding="{StaticResource SettingsPageVerticalPadding}">
    <StackPanel Spacing="4">
        <TextBlock x:Uid="DataStorage_PageTitle"
                   Text="数据与存储"
                   Style="{StaticResource SettingsPageTitleTextStyle}" />
        <TextBlock x:Uid="DataStorage_PageSummary"
                   Text="管理本地历史、缓存、导出和本地数据清理。"
                   Style="{StaticResource SettingsPageSummaryTextStyle}" />
    </StackPanel>

    <StackPanel Spacing="8">
        <TextBlock x:Uid="DataStorage_PrivacyTitle"
                   Text="隐私"
                   Style="{StaticResource SettingsSectionTitleTextStyle}" />
        <Border Style="{StaticResource SettingsSectionContainerStyle}">
            <Grid Style="{StaticResource SettingsRowGridStyle}"
                  ColumnDefinitions="*,Auto">
                <StackPanel Spacing="3">
                    <TextBlock x:Uid="DataStorage_SaveLocalHistory"
                               Text="保存本地历史"
                               Style="{StaticResource SettingsRowTitleTextStyle}" />
                    <TextBlock x:Uid="DataStorage_SaveLocalHistoryDescription"
                               Text="允许 SalmonEgg 保留本地会话历史。"
                               Style="{StaticResource SettingsRowDescriptionTextStyle}" />
                </StackPanel>
                <ToggleSwitch Grid.Column="1"
                              IsOn="{x:Bind ViewModel.Preferences.SaveLocalHistory, Mode=TwoWay}"
                              OnContent="开启"
                              OffContent="关闭" />
            </Grid>
        </Border>
    </StackPanel>

    <StackPanel Spacing="8">
        <TextBlock x:Uid="DataStorage_CacheTitle"
                   Text="缓存"
                   Style="{StaticResource SettingsSectionTitleTextStyle}" />
        <Border Style="{StaticResource SettingsSectionContainerStyle}">
            <StackPanel>
                <Grid Style="{StaticResource SettingsRowGridStyle}"
                      ColumnDefinitions="*,Auto">
                    <StackPanel Spacing="3">
                        <TextBlock x:Uid="DataStorage_CacheRetentionLabel"
                                   Text="缓存保留天数"
                                   Style="{StaticResource SettingsRowTitleTextStyle}" />
                        <TextBlock x:Uid="DataStorage_CacheRetentionDescription"
                                   Text="缓存会在超过保留期后清理。"
                                   Style="{StaticResource SettingsRowDescriptionTextStyle}" />
                    </StackPanel>
                    <NumberBox Grid.Column="1"
                               Value="{x:Bind ViewModel.Preferences.CacheRetentionDays, Mode=TwoWay}"
                               Minimum="1"
                               Maximum="60"
                               SmallChange="1"
                               SpinButtonPlacementMode="Inline"
                               Width="140" />
                </Grid>
                <MenuFlyoutSeparator Margin="16,0" />
                <Grid Style="{StaticResource SettingsRowGridStyle}"
                      ColumnDefinitions="*,Auto">
                    <StackPanel Spacing="3">
                        <TextBlock x:Uid="DataStorage_CacheActionsTitle"
                                   Text="缓存操作"
                                   Style="{StaticResource SettingsRowTitleTextStyle}" />
                        <TextBlock x:Uid="DataStorage_CacheActionsDescription"
                                   Text="清理缓存或打开缓存目录。"
                                   Style="{StaticResource SettingsRowDescriptionTextStyle}" />
                    </StackPanel>
                    <StackPanel Grid.Column="1" Orientation="Horizontal" Spacing="8">
                        <Button x:Uid="DataStorage_ClearCache"
                                Content="清理缓存"
                                Foreground="{ThemeResource SystemFillColorCriticalBrush}"
                                Click="OnClearCacheClick" />
                        <Button x:Uid="DataStorage_OpenCacheFolder"
                                Content="打开缓存目录"
                                Command="{x:Bind ViewModel.OpenCacheFolderCommand}"
                                IsEnabled="{x:Bind ViewModel.CanOpenExternalFiles, Mode=OneWay}" />
                    </StackPanel>
                </Grid>
            </StackPanel>
        </Border>
    </StackPanel>
</StackPanel>
```

Then add the export and danger sections described in the next steps inside the same page stack.

- [ ] **Step 4: Add export command section**

Add:

```xml
<StackPanel Spacing="8">
    <TextBlock x:Uid="DataStorage_ExportTitle"
               Text="导出"
               Style="{StaticResource SettingsSectionTitleTextStyle}" />
    <Border Style="{StaticResource SettingsSectionContainerStyle}">
        <Grid Style="{StaticResource SettingsRowGridStyle}"
              ColumnDefinitions="*,Auto">
            <StackPanel Spacing="3">
                <TextBlock x:Uid="DataStorage_ExportDescription"
                           Text="导出会话和诊断包"
                           Style="{StaticResource SettingsRowTitleTextStyle}" />
                <TextBlock x:Uid="DataStorage_ExportSummary"
                           Text="导出当前会话（Markdown/JSON）与诊断包到 exports 目录。"
                           Style="{StaticResource SettingsRowDescriptionTextStyle}" />
            </StackPanel>
            <StackPanel Grid.Column="1" Spacing="8">
                <StackPanel Orientation="Horizontal" Spacing="8">
                    <Button x:Uid="DataStorage_ExportMarkdown"
                            Content="导出会话（Markdown）"
                            Command="{x:Bind ViewModel.ExportCurrentSessionMarkdownCommand}"
                            IsEnabled="{x:Bind ViewModel.CanExportLocalFiles, Mode=OneWay}" />
                    <Button x:Uid="DataStorage_ExportJson"
                            Content="导出会话（JSON）"
                            Command="{x:Bind ViewModel.ExportCurrentSessionJsonCommand}"
                            IsEnabled="{x:Bind ViewModel.CanExportLocalFiles, Mode=OneWay}" />
                </StackPanel>
                <StackPanel Orientation="Horizontal" Spacing="8">
                    <Button x:Uid="DataStorage_ExportDiagnostics"
                            Content="导出诊断包（ZIP）"
                            Command="{x:Bind ViewModel.CreateDiagnosticsBundleCommand}"
                            IsEnabled="{x:Bind ViewModel.CanExportLocalFiles, Mode=OneWay}" />
                    <Button x:Uid="DataStorage_OpenExports"
                            Content="打开导出目录"
                            Command="{x:Bind ViewModel.OpenExportsFolderCommand}"
                            IsEnabled="{x:Bind ViewModel.CanOpenExternalFiles, Mode=OneWay}" />
                </StackPanel>
            </StackPanel>
        </Grid>
    </Border>
</StackPanel>
```

- [ ] **Step 5: Move danger actions into Expander**

Add:

```xml
<Expander x:Uid="DataStorage_DangerTitle"
          Header="危险操作"
          HorizontalAlignment="Stretch"
          HorizontalContentAlignment="Stretch">
    <StackPanel Spacing="8" Padding="0,8,0,0">
        <TextBlock x:Uid="DataStorage_DangerWarning"
                   Text="以下操作会影响偏好设置或删除本地文件，请确认当前会话已保存。"
                   Style="{StaticResource SettingsRowDescriptionTextStyle}" />
        <Border Style="{StaticResource SettingsSectionContainerStyle}">
            <StackPanel>
                <Grid Style="{StaticResource SettingsRowGridStyle}"
                      ColumnDefinitions="*,Auto">
                    <StackPanel Spacing="3">
                        <TextBlock x:Uid="DataStorage_ResetDefaultsHint"
                                   Text="仅恢复常规、外观、数据与存储、快捷键等偏好，不删除日志或会话导出。"
                                   Style="{StaticResource SettingsRowDescriptionTextStyle}" />
                    </StackPanel>
                    <Button Grid.Column="1"
                            x:Uid="DataStorage_ResetDefaults"
                            Content="恢复默认设置"
                            Click="OnResetPreferencesClick" />
                </Grid>
                <MenuFlyoutSeparator Margin="16,0" />
                <Grid Style="{StaticResource SettingsRowGridStyle}"
                      ColumnDefinitions="*,Auto">
                    <StackPanel Spacing="3">
                        <TextBlock x:Uid="DataStorage_ClearAllDataHint"
                                   Text="删除配置、日志、缓存、导出等文件。此操作不可撤销。"
                                   Style="{StaticResource SettingsRowDescriptionTextStyle}" />
                    </StackPanel>
                    <Button Grid.Column="1"
                            x:Uid="DataStorage_ClearAllData"
                            Content="清空所有本地数据"
                            Foreground="{ThemeResource SystemFillColorCriticalBrush}"
                            Click="OnClearAllLocalDataClick" />
                </Grid>
            </StackPanel>
        </Border>
    </StackPanel>
</Expander>
```

- [ ] **Step 6: Run tests**

Run:

```powershell
dotnet test tests/SalmonEgg.Presentation.Core.Tests/SalmonEgg.Presentation.Core.Tests.csproj --filter "FullyQualifiedName~XamlComplianceTests.DataStorageSettingsPage_SeparatesRoutineStorageAndDangerActions|FullyQualifiedName~XamlComplianceTests.SettingsSubPages_ExposePageTitlesAndSummaries"
```

Expected: data/storage test PASS. The all-subpages title/summary test still FAILS until remaining pages are updated.

- [ ] **Step 7: Commit**

```powershell
git add SalmonEgg/SalmonEgg/Presentation/Views/Settings/DataStorageSettingsPage.xaml tests/SalmonEgg.Presentation.Core.Tests/Ui/XamlComplianceTests.cs
git commit -m "style(settings): refine data storage layout"
```

---

### Task 6: Refactor Shortcuts, Diagnostics, And About Pages

**Files:**
- Modify: `SalmonEgg/SalmonEgg/Presentation/Views/Settings/ShortcutsSettingsPage.xaml`
- Modify: `SalmonEgg/SalmonEgg/Presentation/Views/Settings/DiagnosticsSettingsPage.xaml`
- Modify: `SalmonEgg/SalmonEgg/Presentation/Views/Settings/AboutPage.xaml`
- Modify: `tests/SalmonEgg.Presentation.Core.Tests/Ui/XamlComplianceTests.cs`
- Modify: `tests/SalmonEgg.Presentation.Core.Tests/Settings/DiagnosticsSettingsPageXamlTests.cs`

- [ ] **Step 1: Add diagnostics disclosure assertion**

Append to `DiagnosticsSettingsPageXamlTests`:

```csharp
[Fact]
public void DiagnosticsSettingsPage_LiveLogViewer_RemainsBehindNativeExpander()
{
    var xaml = LoadFile(@"SalmonEgg\SalmonEgg\Presentation\Views\Settings\DiagnosticsSettingsPage.xaml");

    Assert.Contains("<Expander", xaml, StringComparison.Ordinal);
    Assert.Contains("IsExpanded=\"{x:Bind ViewModel.LiveLogViewer.IsExpanded, Mode=TwoWay}\"", xaml, StringComparison.Ordinal);
    Assert.Contains("TextChanged=\"OnLiveLogTextChanged\"", xaml, StringComparison.Ordinal);
}
```

- [ ] **Step 2: Run tests to verify page title/summary contract still fails**

Run:

```powershell
dotnet test tests/SalmonEgg.Presentation.Core.Tests/SalmonEgg.Presentation.Core.Tests.csproj --filter "FullyQualifiedName~XamlComplianceTests.SettingsSubPages_ExposePageTitlesAndSummaries|FullyQualifiedName~DiagnosticsSettingsPageXamlTests.DiagnosticsSettingsPage_LiveLogViewer_RemainsBehindNativeExpander"
```

Expected: title/summary test FAILS. Diagnostics expander test likely PASSES before layout refactor and must continue passing.

- [ ] **Step 3: Update `ShortcutsSettingsPage.xaml`**

Wrap existing content in:

```xml
<StackPanel Spacing="24" Padding="{StaticResource SettingsPageVerticalPadding}">
    <StackPanel Spacing="4">
        <TextBlock x:Uid="Shortcuts_PageTitle"
                   Text="快捷键"
                   Style="{StaticResource SettingsPageTitleTextStyle}" />
        <TextBlock x:Uid="Shortcuts_PageSummary"
                   Text="自定义 SalmonEgg 前台窗口内可用的快捷键。"
                   Style="{StaticResource SettingsPageSummaryTextStyle}" />
    </StackPanel>

    <!-- Keep Shortcuts_ConflictInfo and Shortcuts_InvalidInfo here. -->

    <StackPanel Spacing="8">
        <TextBlock x:Uid="Shortcuts_CustomTitle"
                   Text="自定义"
                   Style="{StaticResource SettingsSectionTitleTextStyle}" />
        <Border Style="{StaticResource SettingsSectionContainerStyle}">
            <StackPanel>
                <Grid Style="{StaticResource SettingsRowGridStyle}">
                    <TextBlock x:Uid="Shortcuts_AppOnlyHint"
                               Text="这些快捷键仅在 SalmonEgg 窗口处于前台时生效，不会注册系统级热键，也不会覆盖输入框内的原生编辑行为。"
                               Style="{StaticResource SettingsRowDescriptionTextStyle}" />
                </Grid>
                <ListView ItemsSource="{x:Bind ViewModel.Shortcuts, Mode=OneWay}"
                          SelectionMode="None">
                    <ListView.ItemTemplate>
                        <DataTemplate x:DataType="vm:ShortcutEntryViewModel">
                            <Grid Style="{StaticResource SettingsRowGridStyle}"
                                  ColumnDefinitions="*,220,Auto">
                                <StackPanel Spacing="2">
                                    <TextBlock Text="{x:Bind Name, Mode=OneWay}"
                                               Style="{StaticResource SettingsRowTitleTextStyle}" />
                                    <TextBlock Text="{x:Bind DefaultGesture, Mode=OneWay}"
                                               Style="{StaticResource SettingsRowDescriptionTextStyle}" />
                                </StackPanel>
                                <controls:ShortcutRecorder Grid.Column="1"
                                                           Gesture="{x:Bind Gesture, Mode=TwoWay}"
                                                           RecorderAutomationId="{x:Bind RecorderAutomationId, Mode=OneWay}"
                                                           x:Uid="Shortcuts_GestureRecorder"
                                                           HorizontalAlignment="Stretch" />
                                <Button Grid.Column="2"
                                        x:Uid="Shortcuts_RestoreSingle"
                                        Content="恢复"
                                        Command="{x:Bind RestoreDefaultCommand}" />
                            </Grid>
                        </DataTemplate>
                    </ListView.ItemTemplate>
                </ListView>
                <Grid Style="{StaticResource SettingsRowGridStyle}"
                      ColumnDefinitions="*,Auto">
                    <TextBlock x:Uid="Shortcuts_RestoreAllDescription"
                               Text="恢复所有快捷键到默认值。"
                               Style="{StaticResource SettingsRowDescriptionTextStyle}" />
                    <Button Grid.Column="1"
                            x:Uid="Shortcuts_RestoreAll"
                            Content="全部恢复默认"
                            Command="{x:Bind ViewModel.RestoreDefaultsCommand}" />
                </Grid>
            </StackPanel>
        </Border>
    </StackPanel>
</StackPanel>
```

- [ ] **Step 4: Update `DiagnosticsSettingsPage.xaml`**

Add page title/summary, convert environment and connection facts into `Grid` rows using shared row styles, keep logs actions as command rows, and preserve the live log `Expander` with the exact binding:

```xml
<Expander IsExpanded="{x:Bind ViewModel.LiveLogViewer.IsExpanded, Mode=TwoWay}"
          Background="Transparent"
          HorizontalAlignment="Stretch"
          HorizontalContentAlignment="Stretch"
          Padding="0">
```

The page title block must be:

```xml
<StackPanel Spacing="4">
    <TextBlock x:Uid="Diagnostics_PageTitle"
               Text="诊断与日志"
               Style="{StaticResource SettingsPageTitleTextStyle}" />
    <TextBlock x:Uid="Diagnostics_PageSummary"
               Text="查看运行环境、日志、连接状态并生成诊断包。"
               Style="{StaticResource SettingsPageSummaryTextStyle}" />
</StackPanel>
```

- [ ] **Step 5: Update `AboutPage.xaml`**

Add page title/summary:

```xml
<StackPanel Spacing="4">
    <TextBlock x:Uid="About_PageTitle"
               Text="关于"
               Style="{StaticResource SettingsPageTitleTextStyle}" />
    <TextBlock x:Uid="About_PageSummary"
               Text="查看版本、支持入口和开源鸣谢。"
               Style="{StaticResource SettingsPageSummaryTextStyle}" />
</StackPanel>
```

Convert app info and support into row layouts. Keep:

```xml
<ListView ItemsSource="{x:Bind ViewModel.OpenSourceAcknowledgements, Mode=OneWay}"
          SelectionMode="None"
          MaxHeight="360"
          AutomationProperties.AutomationId="About.OpenSourceAcknowledgements">
```

- [ ] **Step 6: Run tests**

Run:

```powershell
dotnet test tests/SalmonEgg.Presentation.Core.Tests/SalmonEgg.Presentation.Core.Tests.csproj --filter "FullyQualifiedName~XamlComplianceTests.SettingsSubPages_ExposePageTitlesAndSummaries|FullyQualifiedName~XamlComplianceTests.AboutPage_DisplaysGeneratedOpenSourceAcknowledgements|FullyQualifiedName~DiagnosticsSettingsPageXamlTests"
```

Expected: PASS.

- [ ] **Step 7: Commit**

```powershell
git add SalmonEgg/SalmonEgg/Presentation/Views/Settings/ShortcutsSettingsPage.xaml SalmonEgg/SalmonEgg/Presentation/Views/Settings/DiagnosticsSettingsPage.xaml SalmonEgg/SalmonEgg/Presentation/Views/Settings/AboutPage.xaml tests/SalmonEgg.Presentation.Core.Tests/Ui/XamlComplianceTests.cs tests/SalmonEgg.Presentation.Core.Tests/Settings/DiagnosticsSettingsPageXamlTests.cs
git commit -m "style(settings): refine support diagnostics and shortcuts"
```

---

### Task 7: Final Verification

**Files:**
- No source files expected unless verification exposes defects.

- [ ] **Step 1: Run targeted settings and XAML tests**

Run:

```powershell
dotnet test tests/SalmonEgg.Presentation.Core.Tests/SalmonEgg.Presentation.Core.Tests.csproj --filter "FullyQualifiedName~Settings|FullyQualifiedName~XamlComplianceTests"
```

Expected: PASS.

- [ ] **Step 2: Run full Presentation Core tests**

Run:

```powershell
dotnet test tests/SalmonEgg.Presentation.Core.Tests/SalmonEgg.Presentation.Core.Tests.csproj
```

Expected: PASS.

- [ ] **Step 3: Run app build**

Run:

```powershell
dotnet build SalmonEgg/SalmonEgg/SalmonEgg.csproj
```

Expected: build succeeds with no new warnings related to settings XAML resources, Uno unsupported properties, or binding generation.

- [ ] **Step 4: Inspect final diff**

Run:

```powershell
git diff --stat
git diff -- SalmonEgg/SalmonEgg/App.xaml SalmonEgg/SalmonEgg/Presentation/Views/GeneralSettingsPage.xaml SalmonEgg/SalmonEgg/Presentation/Views/Settings/AppearanceSettingsPage.xaml SalmonEgg/SalmonEgg/Presentation/Views/Settings/AcpConnectionSettingsPage.xaml SalmonEgg/SalmonEgg/Presentation/Views/Settings/DataStorageSettingsPage.xaml SalmonEgg/SalmonEgg/Presentation/Views/Settings/ShortcutsSettingsPage.xaml SalmonEgg/SalmonEgg/Presentation/Views/Settings/DiagnosticsSettingsPage.xaml SalmonEgg/SalmonEgg/Presentation/Views/Settings/AboutPage.xaml
```

Expected: only settings UX/layout and associated tests changed. No ViewModel ownership changes, no new platform checks, no custom control template replacements.

- [ ] **Step 5: Commit verification fixes if needed**

If verification required small fixes, commit them:

```powershell
git add SalmonEgg/SalmonEgg tests/SalmonEgg.Presentation.Core.Tests
git commit -m "fix(settings): address settings layout verification"
```

If no fixes were needed, do not create an empty commit.

---

## Self-Review

- Spec coverage: top navigation preserved by Task 2; page titles/summaries by Tasks 2-6; settings rows by Tasks 1, 3-6; command placement by Tasks 4-6; progressive disclosure by Tasks 4-6; native behavior guardrails by Tasks 1, 2, and final diff inspection.
- Placeholder scan: no `TBD`, `TODO`, or unspecified "handle later" items remain.
- Type consistency: all referenced files, x:Uid names, command names, ViewModel properties, and test method names match existing code or are introduced in the same task.

