# 会话/导航/搜索硬约束（锁定版）

本文件是“行为级硬约束”。目标：锁死会话切换、导航选择、全局搜索的架构与运行语义，禁止后续回归为“修修补补”。

## 1. SSOT 与所有权（必须）
1. 会话激活唯一 owner 必须是 `INavigationCoordinator -> IConversationSessionSwitcher` 链路。
2. View / Adapter 只能表达“用户意图”，不得直接成为会话真实状态来源。
3. 任何“预览态/加载态”必须是投影态，不能替代 SSOT。

## 2. 最新意图语义（Latest-Intent）（必须）
1. 用户最后一次点击的目标会话是唯一有效意图。
2. 旧请求取消/失败不得把 UI 选择回滚到更早会话。
3. 仅当“最新目标会话已无效/不存在”时允许回滚到安全兜底（如 Start）。

## 3. 会话切换状态机（必须）
1. 必须显式分阶段并可落日志：`Selecting -> Selected -> RemoteConnectionReady -> Hydrated | Faulted`。
2. 阶段推进必须是单向，禁止 UI 线程等待远程水合完成。
3. 任何阶段失败必须可诊断（结构化日志，包含 `ConversationId`、`ActivationVersion`、`Reason`）。

## 4. 导航与内容同步（必须）
1. 左侧导航选中态与主内容区必须通过 Coordinator 同步，不允许双写。
2. NavigationView 的视觉状态变化不得反向改写业务状态。
3. 导航失败后的恢复逻辑不得违反 latest-intent 规则。

## 5. 全局搜索状态机（必须）
1. 搜索状态必须显式：`Idle / Loading / Results / Empty / Error`。
2. 必须实现 latest-wins：旧查询结果或旧异常不得覆盖新查询状态。
3. 进入 `Loading` 时必须立即给出可视反馈（pill 或等价反馈）。
4. 搜索计算必须在可取消异步链路中运行，禁止阻塞 UI 线程。

## 6. ACP 协议一致性（必须）
1. 协议行为以官方规范为准，禁止凭记忆新增“隐式协议”。
2. 能力门控必须严格遵守：未声明能力不得调用对应方法（例如 `session/load`）。
3. 对可选字段必须做“存在即解析，不存在不伪造”。
4. 协议相关改动必须在 PR/交付说明中标注“依据的协议条目”。

## 7. 测试与验收门禁（必须）
1. 必须覆盖结果导向测试，不测试实现细节字符串。
2. 至少包含：
   - latest-intent 不回滚回归测试；
   - stale success/error 不覆盖最新查询的搜索测试；
   - 会话切换 UI 响应性 smoke（远程首进 + 快速切换）。
3. 合并前必须通过：
   - `dotnet build`（Windows + Core target）；
   - 目标测试集；
   - GUI smoke。

## 8. 禁止事项（必须）
1. 禁止在 View code-behind 写业务状态机。
2. 禁止同步阻塞（`.Result/.Wait`）进入切换/搜索主链路。
3. 禁止“失败即强制回滚到旧会话”的默认策略。
4. 禁止新增未落测试的关键并发/状态逻辑。

