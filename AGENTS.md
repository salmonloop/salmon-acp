# SalmonEgg Agent 指南

本文件定义本仓库中 AI/Agent 的工作规则。若与其他文档冲突，以本文为准。

## 1. 目标与原则
1. 保持跨平台一致性，避免平台特定行为泄漏到业务逻辑。
2. 代码可读、可维护、可测试优先于“快速修补”。
3. 优先使用框架默认能力与系统排版，避免像素级 hack。

## 2. 必须遵循的规范
1. 代码规范与强约束：`docs/coding-standards.md`
2. 构建与运行指南：`BUILD_GUIDE.md`
3. 代码审查与提交习惯（如有）：`README.md`
4. 严格遵循 MVVM 开发模式，View 完全由 ViewModel 驱动
5. 会话/导航/搜索行为硬约束：`docs/hard-constraints-session-navigation-and-search.md`
6. 规划模式三原则（构建 / 重构 / 调试前必做）：
    - 构建功能前，先思考其架构；
    - 重构代码前，先明确最终理想状态；
    - 调试修复前，先梳理所有已知问题信息。

## 3. 变更策略
1. 先确认问题边界与复现路径，再修改代码。
2. 修改必须最小化影响面，确保跨平台行为一致。
3. 任何偏离规范的实现必须在代码中注明原因，并在变更说明中记录。
4. 除非用户显式要求，否则 **禁止直接重写整个文件**；如果文件被损坏且改动跨度较小，可以用 git 恢复单文件来重新修复，否则需要询问用户。

## 4. 架构与分层（强约束摘要）
1. Core 层：纯 .NET，不允许引用 UI 类型；必须可被跨平台测试引用。
2. UI 层：只做展示与绑定，不包含业务规则。
3. 平台差异：必须集中在平台服务或 `#if`，禁止散落在业务逻辑中。

## 5. 测试与验证
1. 所有 Core 逻辑必须有单元测试。
2. 测试工程必须跨平台可运行。
3. 如果无法运行测试，必须在输出中明确说明原因。

## 6. 日志与诊断
1. 仅保留可长期存在的业务日志。
2. 诊断日志必须移除或放入 `#if DEBUG`。
3. 禁止字符串插值日志，必须使用结构化模板。

## 7. UI 与 XAML 约束（摘要）
1. 绑定默认使用 `x:Bind`；使用 `Binding` 必须注释原因。
2. 优先系统布局控件；禁止像素微调 hack。
3. 禁止使用 Uno 未实现的属性；若必须用 WinUI-only，需平台条件保护。

## 8. 交付与沟通要求
1. 变更完成后，明确列出修改的文件与原因。
2. 如有风险或未验证项，必须显式说明。
3. 不得引入无关格式化或无意义改动。
4. 每次交付时，必须明确保证编译、测试可通过。

## 9. Uno / WinUI 跨平台目标（强约束）
1. Windows 平台必须使用 WinUI 3。
2. 非 Windows 平台必须使用对应的原生控件实现（由 Uno 平台映射）。
3. 尽量跨平台复用 UI 与业务代码，避免为单一平台编写重复实现。
4. 若使用 WinUI-only API 或属性，必须 `#if WINDOWS` 保护，并提供其它平台可编译的替代路径。
5. 平台差异实现必须集中到平台服务或 `Platforms/` 下，禁止散落在业务逻辑或 ViewModel 中。

## 10. 如果用户让你 commit，必须**严格采用英文 conventional message** 格式
1. 参考：https://www.conventionalcommits.org/en/v1.0.0-beta.4/
2. 要根据 1 准确分类
3. 每次 commit 前务必保证测试覆盖完善并且无报错，尽量减少警告

## 11. Case Study 规则沉淀（必做）
1. 对于重复出现、跨端不一致、或修复超过 1 天仍反复回归的问题，必须沉淀为 case study。
2. case study 默认写入本节，但必须沉淀为通用经验规则，不得停留在“某次事故经过”或“某个页面特例”。
3. 每条经验规则至少包含：触发条件、原生期望行为、禁止做法、验证方式；规则表述必须可执行、可验证，禁止写抽象口号。
4. 若后续需要展开长文分析，可在 `docs/audit/` 建立专题文档，并在本节保留一条通用规则加链接，不在本节堆叠事故叙事。
5. 当前沉淀的通用经验规则：
   - 自适应布局从宽屏进入 `Compact` 时，选中态必须继续由 `NavigationView` 原生选择机制投影；禁止在 ViewModel 或代码后置手动回写祖先高亮。
   - 导航点击引发的 `SelectedItem`、内容区跳转和导航高亮必须同源于一次导航状态变更；禁止用 `Frame.Navigated` 反写 selection、`CurrentShellContent` 启发式或补丁事件二次纠偏。
   - `Project` 这类纯分组项必须保持不可选语义；禁止通过样式、事件或视觉伪装把分组容器变成导航目标。
   - 高频异步回调修改 UI 绑定属性时，必须在 ViewModel 显式封送回 UI 线程；禁止在 `ConfigureAwait(false)` 后的后台线程直接触发 `PropertyChanged`。相关分析见 [线程崩溃分析案例](file:///C:/Users/shang/Project/salmon-acp/docs/audit/2026-04-17-acp-threading-crash-post-mortem.md)。
   - 左侧导航从会话点击到 Chat shell 跳转、远端 hydration、replay drain 完成，必须由同一条 latest-intent 状态机统一拥有；禁止 shell 与 chat 各自维护一套互不知情的激活链路。相关分析见 [左侧导航 SSOT 复盘](file:///C:/Users/shang/Project/salmon-acp/docs/audit/2026-04-18-left-nav-session-activation-ssot-post-mortem.md) 与 [Left Nav Session Lifecycle Race Review](file:///C:/Users/shang/Project/salmon-acp/docs/audit/2026-04-19-left-nav-session-lifecycle-race-review.md)。
   - 远端会话是否可热切回，必须由 `conversation binding + ConnectionInstanceId` 判断真实连接实例是否未变；禁止把普通状态投影变化误当成连接实例变化，从而再次触发 `session/load` 或阻塞式 loading overlay。相关分析见 [ACP 多 session 热切回案例](file:///C:/Users/shang/Project/salmon-acp/docs/audit/2026-04-21-acp-multi-session-warm-return-case-study.md)。
   - 后台 unread 是客户端派生的 UI attention 状态，必须按 `remoteSessionId -> conversation binding` 投影，并在会话重新成为已激活且已完成前台投影后清除；禁止通过重跑 `session/load`、延迟计时器或协议补丁制造提醒。相关分析见 [后台 session attention 案例](file:///C:/Users/shang/Project/salmon-acp/docs/audit/2026-04-22-background-session-attention-case-study.md)。
   - ACP 连接池按 `profileId` 缓存可复用连接，只代表复用能力，不代表 UI 已同时订阅多个 profile 的 `session/update` 流；若产品需要跨 profile 实时提醒，必须新增显式多连接事件汇聚层，禁止假设池中保活连接会自然驱动 unread。相关分析见 [跨 profile background attention 边界案例](file:///C:/Users/shang/Project/salmon-acp/docs/audit/2026-04-23-cross-profile-background-attention-boundary.md)。
   - 用户消息 optimistic bubble、`session/prompt.messageId`、`session/prompt.userMessageId`、`session/update.messageId` 与本地持久化 transcript 必须遵守 ACP acknowledgment 语义：客户端 request id 只代表 in-flight 发送，不代表 agent 已记录；只有 `userMessageId` 或后续 authoritative `session/update.messageId` 才能成为持久化 dedup key。禁止把缺失 `userMessageId` 的 request id 当成已确认消息、禁止在有协议 id 时退回纯文本比对、也禁止在存储层丢失 authoritative id。相关分析见 [ACP user message dedup closure case study](file:///C:/Users/shang/Project/salmon-acp/docs/audit/2026-04-22-acp-user-message-deduplication-closure-case-study.md)。
   - ACP `session/list` 属于 discovery-only 元数据接口；触发条件：warm hydration 后或后台目录刷新；原生期望行为：只更新 title、updatedAt、meta 等发现性字段，`cwd` 必须继续以 `session/new`/`session/load` 已建立的会话上下文为准；禁止做法：用 `session/list.cwd` 回写已激活 conversation 的工作目录、project affinity 或 warm-return 路由依据；验证方式：`session/list` 后 active conversation 的 `cwd` 不漂移，首条 prompt 不触发 `session/new` recovery，项目归类不从已知 project 漂到 `__unclassified__`。
   - WinUI3 GUI smoke 的有效验证对象必须是当前 repo 最近一次 `.tools/run-winui3-msix.ps1 -Configuration Debug` 成功安装并写入 provenance marker 的 MSIX；触发条件：需要验证安装后的 GUI 行为、启动路径或协议结论时；原生期望行为：只验证这一次 Debug 安装产生的 MSIX 实例及其对应 binary；禁止做法：用旧安装包、其他目录/其他构建产物、未写入 provenance marker 的安装结果或手工残留安装替代；验证方式：先确认 provenance marker 与目标包路径/hash 匹配，再从该安装实例启动 GUI smoke，并在 [ACP MSIX GUI verification boundary](file:///C:/Users/shang/Project/salmon-acp/docs/audit/2026-04-24-acp-msix-gui-verification-boundary.md) 中记录完整序列。
