# EventHorizon

[English](README.md) | 中文

EventHorizon 是一个基于 .NET 的编码代理宿主，提供 AGUI API、嵌入式 Web 资源、会话持久化、Provider 动态切换、MCP 集成和 Skill 导入能力。

## 特性

- EventHorizon Runtime 与 AGUI 共用一个 Host
- 每个会话独立保存 Provider 和 Model
- 会话级 Agent 缓存，切换后按需重建
- 配置持久化到 `~/.eventhorizon/appsettings.json`
- 支持 MCP Server 配置与测试
- 支持 Skill 文件夹校验与导入到 `~/.eventhorizon/skills`
- 支持会话删除与 AI 自动生成标题

## 配置加载优先级

配置按以下顺序加载，后者覆盖前者：

1. 内置 `src/EventHorizon/appsettings.json`
2. `~/.eventhorizon/appsettings.json`

首次启动时，如果 `~/.eventhorizon/appsettings.json` 不存在，会自动创建目录并用内置配置初始化。

运行时配置统一绑定到 `IOptions<Configuration.AppOptions>`，并通过 Options 注入方式消费。

`CurrentProvider` 已升级为 `CurrentDefaultProvider`。
旧字段在加载时仍兼容读取，并在保存时只写入 `CurrentDefaultProvider`。

## 启动

```zsh
dotnet run --project src/EventHorizon
```

服务会根据 `AGUI:Urls` 监听，并提供：

- `api/*` Controller API
- 嵌入式静态资源页面
- `/agui` 原始 AGUI 端点

如果当前没有可用 Provider，应用仍可启动，便于先进入配置界面完成配置。

## 会话级 Provider / Model

- 每个会话保存自己的 `ProviderName` 和 `Model`
- 未显式选择 Provider 时回退到 `CurrentDefaultProvider`
- 未显式选择 Model 时回退到当前 Provider 默认模型
- 切换 Provider / Model 只影响当前会话
- 切换不会清空聊天记录、标题、摘要或其他会话状态
- Agent 按会话缓存，仅在切换或失效时重建

## AGUI DTO 规则

AGUI 边界 DTO 统一位于 `src/EventHorizon/AGUI/DTOs/`，并且类型名统一以 `DTO` 结尾，例如：

- `CreateAGUISessionRequestDTO`
- `UpdateConversationModelRequestDTO`
- `ConversationModelResponseDTO`
- `AGUISessionSummaryDTO`
- `AGUIRunDTO`

## MCP 与 Skill

- MCP Server 可通过 `api/mcp/test` 测试连通性
- Skill 导入前会进行基础校验
- 成功导入的 Skill 会复制到 `~/.eventhorizon/skills`

## 示例

请查看 `samples/README.md` 和 `samples/` 目录，包含：

- 默认配置示例
- 多 Provider 示例
- MCP 示例
- Skill 目录示例

## 开发

```zsh
dotnet format EventHorizon.slnx
dotnet build EventHorizon.slnx
dotnet test EventHorizon.slnx
```
