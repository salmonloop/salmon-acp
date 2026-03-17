# Shell Layout 严格 SSOT MVVM 设计（主壳层 + 左侧导航 + 标题栏 + 搜索框 + 右侧面板）

日期：2026-03-18

## 背景与问题
当前主壳层的响应式布局由 View 与 ViewModel 混合驱动：
- `MainPage.xaml` 通过 `VisualState` 直接设置 `NavigationView.PaneDisplayMode`、搜索框可见性等。
- `MainPage.xaml.cs` 直接写 `OpenPaneLength` 并参与动画。
- `NavigationViewDisplayModeMonitor` 反向把 UI 状态推回 VM。

这导致：
- 非严格 SSOT（单一真实来源），状态存在双写与反向同步。
- 中等尺寸/切换过程中存在状态短暂失真（如 compact 顶部按钮消失）。

本设计目标是将所有“与窗口尺寸相关的状态”收敛到单一 Store，并让 View 成为纯投影。

## 目标
- 严格 SSOT：所有布局结果只来源于 Store/VM。
- 运行时状态不持久化。
- 覆盖范围：左侧导航、标题栏、搜索框、右侧面板及其响应式逻辑。
- 尽量保持原生样式，不做像素级 hack。

## 非目标
- 不做 UI 视觉风格调整。
- 不引入跨平台持久化或本地存储。
- 不改业务导航树/会话逻辑。

## 运行时状态范围（不持久化）
以下状态仅运行时有效，不落盘：`NavPaneDisplayMode`、`IsNavPaneOpen`、`NavOpenPaneLength`、`NavCompactPaneLength`、`SearchBoxVisibility/Width`、`TitleBarPadding/Height`、`RightPanelVisibility/Width`、`RightPanelMode`、窗口尺寸与 Insets。

## 总体方案
采用“布局快照（Snapshot）”模式：
- `ShellLayoutState` 保存原始输入与用户意图。
- `ShellLayoutSnapshot` 保存派生布局结果（唯一 UI 绑定来源）。
- `ShellLayoutPolicy` 纯函数：State -> Snapshot。
- `ShellLayoutReducer` 处理 Action，更新 State 并产出 Snapshot。
- `IShellLayoutStore` 作为 SSOT（MVUX `IState<T>`）。
- UI 仅通过 `ShellLayoutViewModel.Snapshot` 绑定，不直接 set 控件属性。

## Snapshot 生命周期与缓存策略（最佳实践裁决）
- `ShellLayoutState` 仅保存最小源状态（不包含 Snapshot）。
- `ShellLayoutSnapshot` 作为 **只读派生状态** 由 Store 内部统一生成与缓存。
- Store 暴露双通道：`IState<ShellLayoutState>` 与 `IReadOnlyState<ShellLayoutSnapshot>`（或等价只读接口），避免 ViewModel 重新派生产生分叉。
- ViewModel 仅投影 Snapshot，UI 不回写 Snapshot 字段。
示例命名（仅示意）：`IShellLayoutStateStore` + `IShellLayoutSnapshotStore` 或单一 `IShellLayoutStore` 暴露双通道属性。

## 组件与职责
1. `ShellLayoutState`（Core）
   - 原始输入：窗口宽高、标题栏 Insets、右侧面板模式/拖拽意图、用户导航开合意图等。
2. `ShellLayoutSnapshot`（Core）
   - 派生布局结果：
     - 导航：`NavPaneDisplayMode`、`IsNavPaneOpen`、`NavOpenPaneLength`、`NavCompactPaneLength`
     - 标题栏：`TitleBarHeight`、`TitleBarPadding`
     - 搜索框：`SearchBoxVisibility`、`SearchBoxMinWidth/MaxWidth`
     - 右侧面板：`RightPanelVisibility`、`RightPanelWidth`
3. `ShellLayoutAction`（Core）
   - `WindowMetricsChanged`、`TitleBarInsetsChanged`、`NavToggleRequested`、`RightPanelModeChanged`、`RightPanelResizeRequested` 等。
4. `ShellLayoutPolicy`（Core）
   - 负责响应式判断与布局计算，输入 `State`，输出 `Snapshot`。
5. `ShellLayoutReducer`（Core）
   - 处理 Action，更新 State，并调用 Policy 生成 Snapshot。
6. `IShellLayoutStore`（Presentation.Core）
   - MVUX `IState<ShellLayoutState>`，唯一布局真源。
7. `ShellLayoutViewModel`（Presentation.Core）
   - 对 Snapshot 做只读投影，提供 XAML 绑定。
8. `IWindowMetricsProvider`（UI）
   - 捕获 SizeChanged/Insets，发 Action 进 Store。
9. `MainPage.xaml`（UI）
   - 移除 VisualState 对布局的直接控制，全部绑定 `ShellLayoutViewModel`。
10. `MainPage.xaml.cs`（UI）
   - 只负责输入事件转发（按钮、拖拽），不直接写入控件状态作为最终结果。

## Action 语义（核心动作约定）
- `WindowMetricsChanged(width, height, effectiveWidth, effectiveHeight)`：原始窗口尺寸与可用尺寸；每次尺寸变化触发。
- `TitleBarInsetsChanged(left, right, height)`：标题栏 Insets 变化触发（尺寸变化/激活变化）。  
- `NavToggleRequested(source)`：用户点击切换按钮的意图（不保证立刻生效，由 Policy 决定）。
- `RightPanelModeChanged(mode)`：设置右侧面板模式（None/Diff/Todo）。
- `RightPanelResizeRequested(absoluteWidth)`：用户拖拽产生的**偏好宽度**（Reducer 记录为 PreferredWidth，Snapshot 决定最终宽度）。

## 数据流
1. Window/Insets 变化 -> `IWindowMetricsProvider` 发 `WindowMetricsChanged` Action
2. Reducer 更新 `ShellLayoutState`
3. Policy 生成 `ShellLayoutSnapshot`
4. `ShellLayoutViewModel` 投影 Snapshot
5. XAML 绑定更新 UI
6. 用户操作（按钮/拖拽）-> Action -> Reducer -> Snapshot
7. 动画仅使用 Snapshot 的目标值，不回写状态

## 响应式与规则示例（Policy）
- 使用 `WindowWidth` 决定布局等级（Wide/Medium/Narrow）。
- Medium/Narrow 强制 `IsNavPaneOpen = false`（除非明确设计允许临时展开）。
- SearchBox 在 Narrow 隐藏，Wide/Medium 显示并使用不同宽度区间。
- RightPanel 开启时减少主内容有效宽度，Policy 统一考虑可用宽度。
- 所有宽度值进行 clamp（最小/最大）。

## 优先级与冲突解析（Policy 决策表）
| 约束/输入 | 优先级 | 决策 |
| --- | --- | --- |
| 平台/窗口尺寸约束 | 最高 | Narrow/Medium 强制收起导航；SearchBox 在 Narrow 隐藏 |
| 右侧面板开启 | 高 | 按可用宽度重新计算导航与中间区域 |
| 用户导航开合意图 | 中 | 仅在允许的布局等级下生效 |
| 用户拖拽偏好 | 中 | 在硬约束允许范围内优先满足 |
| 动画 | 低 | 仅作为输入，最终以 Snapshot 结果收敛 |

## 用户意图保留策略
- 若在 Narrow/Medium 触发 `NavToggleRequested`，记录意图但不立即展开；
- 当进入 Wide 时可恢复上次意图（默认开启），并更新 Snapshot。

## 右侧面板宽度策略（用户拖拽优先但不破坏硬约束）
- Store 记录 `RightPanelPreferredWidth`（来自 `RightPanelResizeRequested`）。
- Snapshot 计算 `RightPanelWidth = clamp(PreferredWidth, Min, Max, AvailableWidth)`。
- 当窗口可用宽度缩小导致无法满足偏好时，面板自动收缩但保留偏好；恢复宽度后回到偏好值。
- 若可用宽度小于最小宽度，默认隐藏面板；如需紧凑策略应在非目标中另行明确加入。

## UI 绑定改造原则
- 禁止 XAML VisualState 直接设置 `PaneDisplayMode`、`OpenPaneLength` 等核心状态。
- `NavigationView` 只绑定到 `ShellLayoutViewModel` 暴露的 Snapshot 字段。
- `NavigationViewDisplayModeMonitor` 不得触发 Action；若保留，仅允许日志诊断。

## 动画策略
- 动画仅由 UI 层实现，但目标值来自 Snapshot。
- 动画结束不回写状态（避免 UI 反向驱动）。
- 动画中间态不是状态，不进入 Store；如动画被打断，Snapshot 为最终收敛目标。
补充：动画不产生 Action，仅作为 UI 渲染过程遵从 Snapshot。

## 平台差异
- 平台差异仅存在于 `IWindowMetricsProvider` 的实现中（`#if WINDOWS` 等）。
- Core 层不引用任何 UI 类型。

## IWindowMetricsProvider 初始化与频率
- 初次注入：页面 Loaded 后立即发送 `WindowMetricsChanged` 与 `TitleBarInsetsChanged`。
- 更新频率：窗口 SizeChanged 事件直接发送；如频繁 resize，可在 UI 层进行轻量节流（如 16-32ms）。

## 布局等级判定（width vs effectiveWidth）
- Policy 使用 `effectiveWidth` 作为布局等级判定依据（若 unavailable 则回退 `width`）。

## 测试策略
- `ShellLayoutPolicyTests`：输入窗口宽度/面板状态/意图，断言 Snapshot。
- `ShellLayoutReducerTests`：Action 序列 -> State + Snapshot。
- 竞态序列测试示例：
  - `NavToggleRequested` 与 `WindowMetricsChanged` 交错（先意图后 resize / 先 resize 后意图）。
  - `RightPanelResizeRequested` 与 `WindowMetricsChanged` 同帧触发。
  - `RightPanelModeChanged` 与 `NavToggleRequested` 同时触发。
- UI 层可不做 UI 测试（逻辑下沉到 Core）。

## 验收标准
- 任何窗口尺寸变化不再直接驱动 XAML 控件属性。
- 所有布局状态来源于 `ShellLayoutStore`。
- 中等尺寸 compact 状态下导航按钮稳定显示，不再出现“先消失、切到 minimal 再回才恢复”的现象。
- 右侧面板/标题栏/搜索框与窗口尺寸变化一致且可预测。

## 迁移步骤（概览）
1. 引入 Core 的 `ShellLayoutState/Snapshot/Action/Policy/Reducer`。
2. 引入 Store + ViewModel 投影。
3. 替换 MainPage XAML 绑定与移除 VisualState 控制。
4. 接入 WindowMetricsProvider。
5. 调整 MainPage.xaml.cs 输入事件为 Action。
6. 补充 Core 测试（Policy/Reducer）。

## 风险与回滚
- 变更涉及 MainPage 布局控制逻辑，需谨慎验证。
- 回滚策略：保留旧 VisualState 实现的分支/标记，必要时可恢复。
