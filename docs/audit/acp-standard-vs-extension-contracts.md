# ACP 标准与扩展 Contract 分界

## 结论

`session/request_permission` 属于 ACP 标准方法，字段名和语义以官方 schema 为准，不能自定义。

`_interaction.ask_user` 属于产品扩展方法，方法名必须符合 ACP 扩展命名规则，payload 字段由本仓库自行定义并保持单一 contract。

## 规则

- 触发条件：实现或修改 ACP 请求/响应模型时。
- 原生期望行为：标准方法严格对照官方 schema；扩展方法仅遵守 ACP 扩展命名规则。
- 禁止做法：把扩展 payload 当成标准 schema 固定字段，或引入不带 `_` 前缀的扩展方法名。
- 验证方式：标准方法用 schema/行为测试校验；扩展方法用 capability gating 和 contract 测试校验。
