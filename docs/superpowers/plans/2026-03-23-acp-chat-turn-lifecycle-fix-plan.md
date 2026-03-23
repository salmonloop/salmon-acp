# ACP Chat Turn Lifecycle Fix Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 修复 ACP 聊天首轮消息因 `session/new` 慢响应而丢失的问题，并把聊天界面的 “thinking / tool / responding” 状态重构为 reducer 驱动的 turn state，保证 transcript 只承载 ACP 真实内容，UI 只投影 SSOT。

**Architecture:** 严格区分两类事实。第一类是 ACP 协议事实，来源于 `session/new`、`session/prompt`、`agent_message_chunk`、`agent_thought_chunk`、`tool_call`、`tool_call_update`；这些事实要么进入 transcript，要么进入 turn lifecycle state。第二类是 View 投影事实，来源于 `ChatStateProjector`，只负责把 store 中的 authoritative state 映射成 WinUI/Uno 需要的 `x:Bind` 属性；View 和 code-behind 不得再构造 “thinking placeholder message” 混入消息列表。

**Tech Stack:** ACP JSON-RPC, Uno Platform, WinUI 3 XAML, CommunityToolkit.Mvvm, Uno.Extensions.Reactive, xUnit, Moq.

---

## Scope Lock

本计划只处理以下两件事：

1. `session/new` 慢响应导致的新会话首轮 prompt 没有真正发出，最终界面无回复。
2. 聊天 turn 状态显示从 `IsThinking + placeholder bubble` 迁移到显式 turn state + tail status。

本计划明确不处理以下内容，第三方执行时一旦触碰这些范围必须暂停并回报：

1. 左侧导航排序、`LastAccessedAt` / `LastUpdatedAt` 语义、最近访问逻辑。
2. 右上角 ACP 切换失败。
3. ACP mode/config 兼容清理以外的协议扩展改造。
4. 无关 XAML 重排、视觉改版、全局样式重写。

## Protocol Guardrails

执行前必须再次核对以下官方文档，不允许凭印象实现：

1. Session Setup: [https://agentclientprotocol.com/protocol/session-setup](https://agentclientprotocol.com/protocol/session-setup)
2. Prompt Turn: [https://agentclientprotocol.com/protocol/prompt-turn](https://agentclientprotocol.com/protocol/prompt-turn)
3. Content: [https://agentclientprotocol.com/protocol/content](https://agentclientprotocol.com/protocol/content)
4. Tool Calls: [https://agentclientprotocol.com/protocol/tool-calls](https://agentclientprotocol.com/protocol/tool-calls)
5. Session Config Options: [https://agentclientprotocol.com/protocol/session-config-options](https://agentclientprotocol.com/protocol/session-config-options)
6. Schema: [https://agentclientprotocol.com/protocol/schema](https://agentclientprotocol.com/protocol/schema)

实现时必须遵守以下协议结论：

1. 文本回复来自 `session/update` 中的 `agent_message_chunk`，不是某个本地占位逻辑。
2. `agent_thought_chunk` 是瞬态思考信号，不是 transcript 正文。
3. `tool_call` / `tool_call_update` 驱动工具生命周期，不应伪装成普通文本消息。
4. ACP 没有 `isThinking` 布尔协议字段，UI 只能由协议事件和本地命令生命周期推导。
5. `session/new` 必须真正拿到 `sessionId` 才能发送 `session/prompt`；不能像 `session/prompt` 那样仅凭有流量就判成功。

## Evidence Baseline

第三方执行前先记录以下证据，后续每个检查点都要对照：

1. 故障签名 A, 首轮无回复, `session/new` 超时后晚到响应被丢失：`C:\Users\shang\AppData\Local\SalmonEgg\logs\app-20260323.log`
   - `session/new` 发出：`977`
   - 超时报错：`980`
   - 合法延迟响应晚到：`988`
   - 后续只有 `available_commands_update`，没有 `agent_message_chunk`：`990`
2. 参考签名 B, 健康 prompt turn, 说明 ACP 回复流本身是能工作的：`C:\Users\shang\AppData\Local\SalmonEgg\logs\app-20260322.log`
   - `session/prompt` 发出：`587`
   - `agent_message_chunk` 收到：`710`

## Anti-Drift Rules

1. 禁止整文件重写；只能按任务列出的文件做最小修改。
2. ViewModel 不能再直接用 `MessageHistory` 伪造协议消息。
3. XAML 只能通过 `x:Bind` 绑定 projection/VM 属性；不要把业务判断塞进 converter 或 code-behind。
4. 平台差异不得进入 `ChatViewModel`；Windows / Uno 差异只能留在已有 UI 层或平台层。
5. 新增日志必须可长期保留，优先结构化模板；不要再加新的 DEBUG 噪音。
6. 若执行中发现必须改动本计划未列出的关键文件，先停下来补充计划，再继续。

## File Map

### Core / Infrastructure

- Modify: `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Infrastructure\Client\AcpClient.cs`
- Create: `C:\Users\shang\Project\salmon-acp\tests\SalmonEgg.Infrastructure.Tests\Client\AcpClientTests.cs`

### Chat Coordinator / MVUX

- Modify: `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\Services\Chat\IAcpConnectionCommands.cs`
- Modify: `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\Services\Chat\AcpChatCoordinator.cs`
- Modify: `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\Mvux\Chat\ChatState.cs`
- Modify: `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\Mvux\Chat\ChatAction.cs`
- Modify: `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\Mvux\Chat\ChatReducer.cs`
- Create: `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\Mvux\Chat\ChatTurnPhase.cs`
- Create: `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\Mvux\Chat\ActiveTurnState.cs`
- Modify: `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\Services\Chat\ChatStateProjector.cs`

### ViewModel / Persistence

- Modify: `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\ViewModels\Chat\ChatViewModel.cs`
- Modify: `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\ViewModels\Chat\ChatMessageViewModel.cs`
- Modify: `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\Services\Chat\WorkspaceWriter.cs`

### WinUI / Uno Views

- Modify: `C:\Users\shang\Project\salmon-acp\SalmonEgg\SalmonEgg\Styles\ChatStyles.xaml`
- Modify: `C:\Users\shang\Project\salmon-acp\SalmonEgg\SalmonEgg\Presentation\Views\Chat\ChatView.xaml`
- Modify: `C:\Users\shang\Project\salmon-acp\SalmonEgg\SalmonEgg\Presentation\Views\MiniWindow\MiniChatView.xaml`

### Tests

- Modify: `C:\Users\shang\Project\salmon-acp\tests\SalmonEgg.Infrastructure.Tests\SalmonEgg.Infrastructure.Tests.csproj`
- Modify: `C:\Users\shang\Project\salmon-acp\tests\SalmonEgg.Presentation.Core.Tests\Chat\AcpChatCoordinatorTests.cs`
- Modify: `C:\Users\shang\Project\salmon-acp\tests\SalmonEgg.Presentation.Core.Tests\Chat\Mvux\ChatReducerTests.cs`
- Modify: `C:\Users\shang\Project\salmon-acp\tests\SalmonEgg.Presentation.Core.Tests\Services\ChatStateProjectorTests.cs`
- Modify: `C:\Users\shang\Project\salmon-acp\tests\SalmonEgg.Presentation.Core.Tests\Chat\ChatViewModelTests.cs`
- Modify: `C:\Users\shang\Project\salmon-acp\tests\SalmonEgg.Presentation.Core.Tests\Chat\WorkspaceWriterTests.cs`

## Review Gate Template

每个 chunk 完成后，提交物必须包含以下四项，缺一项都不能进入下一个 chunk：

1. `git diff --stat` 输出，只允许出现该 chunk 文件清单中的路径。
2. 该 chunk 指定测试命令的完整通过结果。
3. 一段不超过 8 行的说明：
   - 改了什么
   - 为什么没有偏离计划
   - 还有什么残留风险
4. reviewer 明确勾选：
   - 没有引入 placeholder transcript
   - 没有把业务逻辑塞进 XAML/code-behind
   - 没有改动本计划 scope 外内容

## Chunk 1: Fix `session/new` Timeout Semantics

### Task 1: 为 `session/new` 建立独立 timeout budget，并用可测试方式回归验证

**Files:**
- Modify: `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Infrastructure\Client\AcpClient.cs`
- Create: `C:\Users\shang\Project\salmon-acp\tests\SalmonEgg.Infrastructure.Tests\Client\AcpClientTests.cs`
- Modify: `C:\Users\shang\Project\salmon-acp\tests\SalmonEgg.Infrastructure.Tests\SalmonEgg.Infrastructure.Tests.csproj`

- [ ] **Step 1: 先写失败测试，覆盖本次根因**

至少新增以下测试名称：

```csharp
[Fact]
public async Task CreateSessionAsync_SlowButValidResponse_UsesSessionNewTimeoutBudget()

[Fact]
public async Task NonPromptRequest_StillUsesDefaultTimeoutBudget()

[Fact]
public async Task CreateSessionAsync_TimeoutMessage_ContainsMethodAndLastRx()
```

测试策略必须满足：

1. 不允许真的等待 30 秒或 2 分钟。
2. 通过可注入的 timeout 配置，把默认 budget 压缩到几十毫秒，把 `session/new` budget 设成更长的测试值。
3. 用 fake transport / delayed response 精确复现 “默认 budget 超时，但 `session/new` 专属 budget 下可以成功”。

- [ ] **Step 2: 运行定向测试，确认当前实现先失败**

Run:

```powershell
dotnet test C:\Users\shang\Project\salmon-acp\tests\SalmonEgg.Infrastructure.Tests\SalmonEgg.Infrastructure.Tests.csproj -c Debug --no-restore -nodeReuse:false --filter "FullyQualifiedName~AcpClientTests"
```

Expected:

1. 新增的 `AcpClientTests` 失败。
2. 至少有一个失败明确说明 `CreateSessionAsync` 仍然走默认 30 秒 budget 或无法注入 budget。

- [ ] **Step 3: 在 `AcpClient` 中实现最小修复**

实现约束：

1. 新增一个内部可注入的 timeout 配置对象，例如：

```csharp
internal sealed record AcpRequestTimeouts(
    TimeSpan DefaultTimeout,
    TimeSpan SessionNewTimeout,
    TimeSpan SessionPromptTimeout);
```

2. 生产默认值保持：
   - generic: `30s`
   - `session/new`: `2min`
   - `session/prompt`: `2min`
3. `SendRequestAsync` 内根据 `request.Method` 统一解析 budget，不要把 if/else 散落到各个 public API。
4. `session/prompt` 现有 “有后续 session traffic 则允许 streaming-only 成功” 的兼容逻辑保持不变。
5. 本 chunk 不处理 “timeout 后接纳晚到 `session/new` response” 这类更大语义改造；只修 method-specific timeout 与日志准确性。
6. timeout 文案必须去掉误导性的 MCP 猜测，改成聚焦事实：
   - 哪个 method
   - 等了多久
   - 上次收到流量是什么时候
7. 如果为了测试需要访问内部 timeout 配置，只允许使用测试可见性手段，例如 `InternalsVisibleTo` 或 internal constructor；不要把测试专用配置暴露成面向生产调用方的 public API。

- [ ] **Step 4: 重新运行定向测试，确认通过**

Run:

```powershell
dotnet test C:\Users\shang\Project\salmon-acp\tests\SalmonEgg.Infrastructure.Tests\SalmonEgg.Infrastructure.Tests.csproj -c Debug --no-restore -nodeReuse:false --filter "FullyQualifiedName~AcpClientTests"
```

Expected:

1. `AcpClientTests` 全绿。
2. 不引入其他 Infrastructure 测试回归。

### Chunk 1 Checkpoint

- [ ] **Step 5: 运行该项目全部 Infrastructure 测试**

```powershell
dotnet test C:\Users\shang\Project\salmon-acp\tests\SalmonEgg.Infrastructure.Tests\SalmonEgg.Infrastructure.Tests.csproj -c Debug --no-restore -nodeReuse:false
```

- [ ] **Step 6: 记录 checkpoint 证据**

必须附上：

```powershell
git diff --stat
```

建议提交信息：

```bash
git commit -m "fix(acp): extend session creation timeout budget"
```

## Chunk 2: Split Prompt Dispatch from Session Creation

### Task 2: 把 coordinator 从“一把梭 send”拆成可表达 turn phase 的两步式 API

**Files:**
- Modify: `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\Services\Chat\IAcpConnectionCommands.cs`
- Modify: `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\Services\Chat\AcpChatCoordinator.cs`
- Modify: `C:\Users\shang\Project\salmon-acp\tests\SalmonEgg.Presentation.Core.Tests\Chat\AcpChatCoordinatorTests.cs`

- [ ] **Step 1: 先写失败测试，锁定新的 coordinator seam**

至少新增以下测试名称：

```csharp
[Fact]
public async Task DispatchPromptToRemoteSessionAsync_UsesProvidedRemoteSessionId()

[Fact]
public async Task DispatchPromptToRemoteSessionAsync_RemoteSessionNotFound_RecreatesSessionAndRetriesOnce()
```

如果为了兼容保留原 `SendPromptAsync`，再补一个 wrapper 测试：

```csharp
[Fact]
public async Task SendPromptAsync_DelegatesToEnsureAndDispatchFlow()
```

- [ ] **Step 2: 运行定向测试，确认当前实现先失败**

Run:

```powershell
dotnet test C:\Users\shang\Project\salmon-acp\tests\SalmonEgg.Presentation.Core.Tests\SalmonEgg.Presentation.Core.Tests.csproj -c Debug --no-restore -nodeReuse:false --filter "FullyQualifiedName~AcpChatCoordinatorTests"
```

Expected:

1. 新增 coordinator seam 的测试失败。
2. 失败原因应表明当前 API 还把 “创建远端 session” 和 “发送 prompt” 混在一起。

- [ ] **Step 3: 提取显式的 prompt dispatch API**

实现要求：

1. 在 `IAcpConnectionCommands` 增加显式方法，例如：

```csharp
Task<AcpPromptDispatchResult> DispatchPromptToRemoteSessionAsync(
    string remoteSessionId,
    string promptText,
    IAcpChatCoordinatorSink sink,
    Func<CancellationToken, Task<bool>> authenticateAsync,
    CancellationToken cancellationToken = default);
```

2. `EnsureRemoteSessionAsync` 继续只负责拿到 authoritative `remoteSessionId` 并更新 binding。
3. `DispatchPromptToRemoteSessionAsync` 只负责：
   - 用给定 `remoteSessionId` 发送 `session/prompt`
   - 处理 auth retry
   - 如果 `session not found`，清 binding、重建 remote session、重试一次并返回 `RetriedAfterSessionRecovery=true`
4. 不在这一层创建任何 UI placeholder 或 turn 文本。
5. 如果保留现有 `SendPromptAsync`，它只能是一个薄封装，内部调用 `EnsureRemoteSessionAsync` + `DispatchPromptToRemoteSessionAsync`。

- [ ] **Step 4: 重新运行定向测试，确认通过**

Run:

```powershell
dotnet test C:\Users\shang\Project\salmon-acp\tests\SalmonEgg.Presentation.Core.Tests\SalmonEgg.Presentation.Core.Tests.csproj -c Debug --no-restore -nodeReuse:false --filter "FullyQualifiedName~AcpChatCoordinatorTests"
```

### Chunk 2 Checkpoint

- [ ] **Step 5: 确认 coordinator 改动没有触碰 UI 逻辑**

核对：

1. `AcpChatCoordinator.cs` 里不能出现 `MessageHistory` / `IsThinking` / XAML 术语。
2. `ChatViewModel` 尚未改动也应该能编译。

- [ ] **Step 6: 记录 checkpoint 证据**

```powershell
git diff --stat
```

建议提交信息：

```bash
git commit -m "refactor(chat): split session creation from prompt dispatch"
```

## Chunk 3: Introduce Explicit Turn State in MVUX Store

### Task 3: 用 `ActiveTurnState + ChatTurnPhase` 替代 `IsThinking`

**Files:**
- Create: `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\Mvux\Chat\ChatTurnPhase.cs`
- Create: `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\Mvux\Chat\ActiveTurnState.cs`
- Modify: `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\Mvux\Chat\ChatState.cs`
- Modify: `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\Mvux\Chat\ChatAction.cs`
- Modify: `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\Mvux\Chat\ChatReducer.cs`
- Modify: `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\Services\Chat\ChatStateProjector.cs`
- Modify: `C:\Users\shang\Project\salmon-acp\tests\SalmonEgg.Presentation.Core.Tests\Chat\Mvux\ChatReducerTests.cs`
- Modify: `C:\Users\shang\Project\salmon-acp\tests\SalmonEgg.Presentation.Core.Tests\Services\ChatStateProjectorTests.cs`

- [ ] **Step 1: 先写 reducer / projector 的失败测试**

至少新增以下测试名称：

```csharp
[Fact]
public void BeginTurn_SetsActiveTurnAndGeneration()

[Fact]
public void AdvanceTurnPhase_IgnoresStaleTurnId()

[Fact]
public void SelectConversation_ClearsActiveTurnForPreviousConversation()

[Fact]
public void Apply_ProjectsTailStatusFromActiveTurn()
```

- [ ] **Step 2: 运行定向测试，确认先失败**

Run:

```powershell
dotnet test C:\Users\shang\Project\salmon-acp\tests\SalmonEgg.Presentation.Core.Tests\SalmonEgg.Presentation.Core.Tests.csproj -c Debug --no-restore -nodeReuse:false --filter "FullyQualifiedName~ChatReducerTests"
dotnet test C:\Users\shang\Project\salmon-acp\tests\SalmonEgg.Presentation.Core.Tests\SalmonEgg.Presentation.Core.Tests.csproj -c Debug --no-restore -nodeReuse:false --filter "FullyQualifiedName~ChatStateProjectorTests"
```

- [ ] **Step 3: 建立明确的 turn state 模型**

推荐最小模型：

```csharp
public enum ChatTurnPhase
{
    CreatingRemoteSession,
    WaitingForAgent,
    Thinking,
    ToolPending,
    ToolRunning,
    Responding,
    Completed,
    Failed,
    Cancelled
}

public sealed record ActiveTurnState(
    string ConversationId,
    string TurnId,
    ChatTurnPhase Phase,
    DateTime StartedAtUtc,
    DateTime LastUpdatedAtUtc,
    string? ToolCallId = null,
    string? ToolTitle = null,
    string? FailureMessage = null);
```

实现约束：

1. `ChatState` 新增 `ActiveTurn`，不要再用 `bool IsThinking` 作为 source of truth。
2. `ChatAction` 新增显式动作，例如：
   - `BeginTurnAction`
   - `AdvanceTurnPhaseAction`
   - `CompleteTurnAction`
   - `FailTurnAction`
   - `CancelTurnAction`
   - `ClearTurnAction`
3. reducer 必须忽略 stale conversation / stale turn id。
4. `ChatStateProjector` 新增 projection 字段，例如：
   - `IsTurnStatusVisible`
   - `TurnStatusText`
   - `IsTurnStatusRunning`
   - `TurnPhase`
5. 在本 chunk 结束前，可以临时保留 `projection.IsThinking` 作为兼容投影，但它必须是 `ActiveTurn` 的派生值，不能再来自 store 独立布尔字段。

- [ ] **Step 4: 重新运行定向测试，确认通过**

Run:

```powershell
dotnet test C:\Users\shang\Project\salmon-acp\tests\SalmonEgg.Presentation.Core.Tests\SalmonEgg.Presentation.Core.Tests.csproj -c Debug --no-restore -nodeReuse:false --filter "FullyQualifiedName~ChatReducerTests"
dotnet test C:\Users\shang\Project\salmon-acp\tests\SalmonEgg.Presentation.Core.Tests\SalmonEgg.Presentation.Core.Tests.csproj -c Debug --no-restore -nodeReuse:false --filter "FullyQualifiedName~ChatStateProjectorTests"
```

### Chunk 3 Checkpoint

- [ ] **Step 5: 确认 turn state 仍然是纯 Core / MVUX 逻辑**

核对：

1. 新增文件不引用任何 `Microsoft.UI.Xaml.*` 类型。
2. `ChatStateProjector` 只产出 projection，不含 UI 控件引用。

- [ ] **Step 6: 记录 checkpoint 证据**

```powershell
git diff --stat
```

建议提交信息：

```bash
git commit -m "refactor(chat): model turn lifecycle in mvux state"
```

## Chunk 4: Rewire `ChatViewModel` to Protocol-Driven Turn Transitions

### Task 4: 让 turn phase 只由命令生命周期与 ACP update 推进

**Files:**
- Modify: `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\ViewModels\Chat\ChatViewModel.cs`
- Modify: `C:\Users\shang\Project\salmon-acp\tests\SalmonEgg.Presentation.Core.Tests\Chat\ChatViewModelTests.cs`
- Modify: `C:\Users\shang\Project\salmon-acp\tests\SalmonEgg.Presentation.Core.Tests\Chat\AcpChatCoordinatorTests.cs`

- [ ] **Step 1: 先写失败测试，覆盖发送与 update 生命周期**

至少新增以下测试名称：

```csharp
[Fact]
public async Task SendPromptAsync_WithoutBinding_BeginsTurnAsCreatingRemoteSession()

[Fact]
public async Task SendPromptAsync_AfterRemoteSessionCreated_AdvancesTurnToWaitingForAgent()

[Fact]
public async Task ProcessSessionUpdateAsync_AgentThoughtUpdate_AdvancesTurnToThinking()

[Fact]
public async Task ProcessSessionUpdateAsync_ToolCallStatusUpdate_InProgress_AdvancesTurnToToolRunning()

[Fact]
public async Task ProcessSessionUpdateAsync_AgentMessageUpdate_AdvancesTurnToRespondingAndAppendsTranscript()

[Fact]
public async Task SendPromptAsync_Timeout_FailsTurnAndRestoresDraft()
```

- [ ] **Step 2: 运行定向测试，确认先失败**

Run:

```powershell
dotnet test C:\Users\shang\Project\salmon-acp\tests\SalmonEgg.Presentation.Core.Tests\SalmonEgg.Presentation.Core.Tests.csproj -c Debug --no-restore -nodeReuse:false --filter "FullyQualifiedName~ChatViewModelTests"
```

- [ ] **Step 3: 改造发送链路与 update 链路**

实现要求：

1. `ChatViewModel.SendPromptAsync` 在发送前生成新的本地 `turnId`。
2. 按 authoritative binding 决定初始 phase：
   - 无远端 binding: `CreatingRemoteSession`
   - 有远端 binding: `WaitingForAgent`
3. `ChatViewModel` 先显式调用 `EnsureRemoteSessionAsync`，成功后再调用 coordinator 的 `DispatchPromptToRemoteSessionAsync`。
4. `SendPromptAsync` 的 `finally` 中不能再无条件把 turn 置空，也不能再把 thinking 置 false。
5. `ProcessSessionUpdateAsync` 中原来的 `SetIsThinkingAction` 全部替换为 turn phase action：
   - `AgentThoughtUpdate` -> `Thinking`
   - `ToolCallUpdate` -> `ToolPending`
   - `ToolCallStatusUpdate`:
     - `Pending` -> `ToolPending`
     - `InProgress` -> `ToolRunning`
     - `Completed` -> 回到 `WaitingForAgent`，除非随后马上收到 `agent_message_chunk`
     - `Failed` -> `Failed`
   - `AgentMessageUpdate` -> `Responding`
6. transcript 仍然只写入真实 ACP content / tool snapshots。
7. SSOT gating 保持不变：只有 active authoritative remote binding 对应的 update 才能修改可见 transcript 和 turn state。

- [ ] **Step 4: 明确 turn 终态规则**

必须显式实现并测试：

1. prompt 发送抛出异常 -> `FailTurnAction`
2. 用户取消 -> `CancelTurnAction`
3. 成功完整结束 -> `CompleteTurnAction`
4. 切换会话 -> 清除旧会话 active turn

- [ ] **Step 5: 重新运行定向测试，确认通过**

Run:

```powershell
dotnet test C:\Users\shang\Project\salmon-acp\tests\SalmonEgg.Presentation.Core.Tests\SalmonEgg.Presentation.Core.Tests.csproj -c Debug --no-restore -nodeReuse:false --filter "FullyQualifiedName~ChatViewModelTests"
dotnet test C:\Users\shang\Project\salmon-acp\tests\SalmonEgg.Presentation.Core.Tests\SalmonEgg.Presentation.Core.Tests.csproj -c Debug --no-restore -nodeReuse:false --filter "FullyQualifiedName~AcpChatCoordinatorTests"
```

### Chunk 4 Checkpoint

- [ ] **Step 6: 做一次最小人工日志验证**

运行应用并复现一次新会话首轮发送，检查当天日志：

1. 必须出现 `session/new`
2. 随后必须出现 `session/prompt`
3. 若 agent 正常响应，后续必须出现 `agent_message_chunk` 或 `agent_thought_chunk`
4. 不应再在首轮 30s 附近出现误判超时

建议使用：

```powershell
Get-Content C:\Users\shang\AppData\Local\SalmonEgg\logs\app-$(Get-Date -Format yyyyMMdd).log -Tail 200
```

- [ ] **Step 7: 记录 checkpoint 证据**

```powershell
git diff --stat
```

建议提交信息：

```bash
git commit -m "fix(chat): drive turn state from acp updates"
```

## Chunk 5: Replace Placeholder Bubble with Tail Status UI

### Task 5: 从 transcript 中彻底移除 “thinking placeholder”，改成 projection 驱动的尾部状态条

**Files:**
- Modify: `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\ViewModels\Chat\ChatViewModel.cs`
- Modify: `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\ViewModels\Chat\ChatMessageViewModel.cs`
- Modify: `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\Services\Chat\ChatStateProjector.cs`
- Modify: `C:\Users\shang\Project\salmon-acp\src\SalmonEgg.Presentation.Core\Services\Chat\WorkspaceWriter.cs`
- Modify: `C:\Users\shang\Project\salmon-acp\SalmonEgg\SalmonEgg\Styles\ChatStyles.xaml`
- Modify: `C:\Users\shang\Project\salmon-acp\SalmonEgg\SalmonEgg\Presentation\Views\Chat\ChatView.xaml`
- Modify: `C:\Users\shang\Project\salmon-acp\SalmonEgg\SalmonEgg\Presentation\Views\MiniWindow\MiniChatView.xaml`
- Modify: `C:\Users\shang\Project\salmon-acp\tests\SalmonEgg.Presentation.Core.Tests\Chat\ChatViewModelTests.cs`
- Modify: `C:\Users\shang\Project\salmon-acp\tests\SalmonEgg.Presentation.Core.Tests\Chat\WorkspaceWriterTests.cs`

- [ ] **Step 1: 先写失败测试，锁定 UI 不再注入伪消息**

至少新增以下测试名称：

```csharp
[Fact]
public async Task SyncMessageHistory_DoesNotAppendPlaceholderWhenTurnStatusVisible()

[Fact]
public void WorkspaceWriter_StillStripsLegacyThinkingSnapshotsOnSave()
```

- [ ] **Step 2: 运行定向测试，确认先失败**

Run:

```powershell
dotnet test C:\Users\shang\Project\salmon-acp\tests\SalmonEgg.Presentation.Core.Tests\SalmonEgg.Presentation.Core.Tests.csproj -c Debug --no-restore -nodeReuse:false --filter "FullyQualifiedName~ChatViewModelTests"
dotnet test C:\Users\shang\Project\salmon-acp\tests\SalmonEgg.Presentation.Core.Tests\SalmonEgg.Presentation.Core.Tests.csproj -c Debug --no-restore -nodeReuse:false --filter "FullyQualifiedName~WorkspaceWriterTests"
```

- [ ] **Step 3: 删除 placeholder transcript 注入**

实现要求：

1. `ChatViewModel.SyncMessageHistory` 只按 transcript 精确同步 `MessageHistory.Count`。
2. 删除 `CreateThinkingPlaceholder` 的调用路径。
3. `ChatMessageViewModel` 去掉 `IsThinkingPlaceholder` 及相关仅为 placeholder 服务的属性。

- [ ] **Step 4: 加 tail status UI，而不是消息气泡**

UI 约束：

1. 在 `ChatView.xaml` 和 `MiniChatView.xaml` 中，于消息列表与输入区之间增加独立状态行。
2. 状态行必须通过 `x:Bind` 绑定 projection/VM 属性，例如：
   - `IsTurnStatusVisible`
   - `TurnStatusText`
   - `IsTurnStatusRunning`
3. 状态行只显示轻量 ProgressRing + TextBlock，不进入 transcript。
4. 颜色、间距、字体全部使用现有 theme resources；不要硬编码颜色。
5. 不要把状态条做成新的 `ListView` item，也不要在 code-behind 手动 show/hide。

- [ ] **Step 5: 保留 `WorkspaceWriter` 的 legacy cleanup 防线**

要求：

1. 即使新的 ViewModel 不再生成 placeholder，也保留 `WorkspaceWriter` 过滤旧 `"thinking"` snapshot 的逻辑，作为历史数据清洗。
2. 在代码注释里说明这是旧版本兼容清理，而不是新的 source of truth。

- [ ] **Step 6: 重新运行定向测试，确认通过**

Run:

```powershell
dotnet test C:\Users\shang\Project\salmon-acp\tests\SalmonEgg.Presentation.Core.Tests\SalmonEgg.Presentation.Core.Tests.csproj -c Debug --no-restore -nodeReuse:false --filter "FullyQualifiedName~ChatViewModelTests"
dotnet test C:\Users\shang\Project\salmon-acp\tests\SalmonEgg.Presentation.Core.Tests\SalmonEgg.Presentation.Core.Tests.csproj -c Debug --no-restore -nodeReuse:false --filter "FullyQualifiedName~WorkspaceWriterTests"
```

### Chunk 5 Checkpoint

- [ ] **Step 7: 人工 UI 验证**

手工确认以下行为：

1. 新会话首轮发送时，消息列表中不再出现“思考中”假气泡。
2. 消息列表下方出现独立状态条。
3. 收到真实 `agent_message_chunk` 后，状态条阶段从 `Thinking/ToolRunning/Responding` 正常推进。
4. 会话切换后，旧会话状态条不会污染新会话。

- [ ] **Step 8: 记录 checkpoint 证据**

```powershell
git diff --stat
```

建议提交信息：

```bash
git commit -m "refactor(chat): replace thinking placeholder with tail status"
```

## Chunk 6: Final Verification and Handoff

### Task 6: 用测试、日志、MSIX 运行三重验证收口

**Files:**
- Verify only

- [ ] **Step 1: 还原并执行完整测试**

```powershell
dotnet restore C:\Users\shang\Project\salmon-acp\SalmonEgg.sln
dotnet test C:\Users\shang\Project\salmon-acp\tests\SalmonEgg.Infrastructure.Tests\SalmonEgg.Infrastructure.Tests.csproj -c Release --no-restore -nodeReuse:false
dotnet test C:\Users\shang\Project\salmon-acp\tests\SalmonEgg.Presentation.Core.Tests\SalmonEgg.Presentation.Core.Tests.csproj -c Release --no-restore -nodeReuse:false
dotnet test C:\Users\shang\Project\salmon-acp\SalmonEgg.sln -c Release --no-restore -nodeReuse:false
```

- [ ] **Step 2: 构建桌面目标**

```powershell
dotnet build C:\Users\shang\Project\salmon-acp\SalmonEgg\SalmonEgg\SalmonEgg.csproj -c Release --framework net10.0-desktop --no-restore -nodeReuse:false
```

- [ ] **Step 3: 验证 `run.bat msix`**

```powershell
cmd /c C:\Users\shang\Project\salmon-acp\run.bat msix
```

说明：

1. 若是首次在该机器信任开发证书，需要按 `BUILD_GUIDE.md` 说明在管理员 PowerShell 中执行。
2. 本步骤必须验证应用能成功启动到聊天界面，而不是只看打包成功。

- [ ] **Step 4: 做最终人工验收**

验收步骤：

1. 打开应用，新建一个会话。
2. 选择 ACP profile。
3. 发送第一条消息。
4. 确认：
   - 首轮消息后能看到真实回复
   - 状态条存在，但 transcript 中没有假 “thinking” 消息
   - 日志中 `session/new` 后能够继续到 `session/prompt`
   - 如 agent 有思考或工具调用，状态条会跟着变化

- [ ] **Step 5: 输出最终交付说明**

最终交付必须包含：

1. 修改文件清单及每个文件的职责变化。
2. 所有测试命令与结果。
3. `run.bat msix` 是否成功。
4. 若仍有残留风险，必须明确列出，不得省略。

建议最终提交信息：

```bash
git commit -m "fix(chat): recover first-turn replies and add protocol-driven turn status"
```

## Stop Conditions

执行中遇到以下任一情况必须暂停，不允许继续“顺手修”：

1. 发现根因其实涉及 `AcpMessageParser` 或协议模型不兼容，而不是 timeout / lifecycle。
2. 发现必须改动左侧导航排序或 `LastAccessedAt` 逻辑才能让本计划通过。
3. 发现 `run.bat msix` 失败原因来自证书、SDK 或签名脚本，与本计划代码无关。
4. 发现 UI 状态条需要新增平台特有 API 才能显示。

## Done Definition

只有同时满足以下条件，才算完成：

1. 新会话首轮发送不会再因 `session/new` 30 秒超时而吞掉回复。
2. 聊天 transcript 中不再出现本地伪造的 “thinking” placeholder。
3. thinking/tool/responding 状态来自 turn state，而不是裸 `IsThinking` 布尔。
4. `dotnet test` 全绿。
5. `cmd /c run.bat msix` 成功启动。
