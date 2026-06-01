EventHorizon
===========

[![Nuget](https://img.shields.io/nuget/v/EventHorizon)](https://www.nuget.org/packages/EventHorizon/)

English | [简体中文](./README.zh-CN.md)

EventHorizon 是一个基于 **Microsoft Agent Framework** 开发的 Code Agent 项目。

---

## 快速上手

### 发行版本（直接使用）

如果你只想直接使用已发布的 EventHorizon 版本，可以通过 .NET Core CLI 进行全局快速安装：

```bash
dotnet tool install --global EventHorizon
```

安装完成后，运行以下命令启动 EventHorizon：

```bash
eventhorizon
```

随后在浏览器中访问：http://localhost:9527 即可看到 Web UI 界面。

提示：首次启动后，请点击 Web UI 右上角的 Settings（设置） 来配置你的模型服务商（Provider）。

### Provider 配置说明

你可以在 Web UI 的 `Settings -> Providers` 中配置 provider。

当前支持的 provider 类型包括：

- `openai`
- `openai-compatible`
- `azure-openai`
- `anthropic`
- `gemini`

每个 provider 条目包含以下基础配置：

- `Name`：Provider 的唯一名称，会显示在 UI 中，也可用于默认选择
- `Type`：Provider 类型
- `Default model`：当会话没有单独覆盖时默认使用的模型
- `Available models`：可选模型列表，会显示在 UI 中，每行一个 model ID

根据不同 provider 类型，还可能需要以下字段：

- `API key`：大多数 provider 需要
- `Endpoint`：`openai-compatible` 和 `azure-openai` 需要
- `Deployment`：`azure-openai` 支持
- `Use default Azure credential`：`azure-openai` 支持；当你不想在本地保存 API key 时可以使用

Provider 行为说明：

- 你可以配置多个 provider，并选择其中一个作为全局默认 provider。
- 每个会话都可以单独覆盖全局 provider 和 model 选择。
- 对于 Azure OpenAI，EventHorizon 会优先使用 `Deployment`，必要时回退到 `Model`。
- 对于 `openai-compatible`，请填写该服务的基础 endpoint，并配置该服务实际支持的 model ID。

配置示例：

- OpenAI
  - Type: `openai`
  - API key: 你的 OpenAI API Key
  - Default model: `gpt-4.1-mini`
- Azure OpenAI
  - Type: `azure-openai`
  - Endpoint: 你的 Azure OpenAI 资源地址
  - Deployment: 你的 deployment 名称
  - Default model: 可选
  - API key: 当启用 `Use default Azure credential` 时可以不填
- Anthropic
  - Type: `anthropic`
  - API key: 你的 Anthropic API Key
  - Default model: `claude-sonnet-4-0`

### Skill 配置说明

你可以在 Web UI 的 `Settings -> Skills` 中配置共享技能。

Skill 通过本地文件夹导入，并保存在共享 skill catalog 中。

全局 skill 配置包括：

- `Skill import path`：要导入的本地 skill 文件夹路径
- `Storage path`：EventHorizon 用来保存 skill 元数据的目录
- `Imported skills`：当前已导入的共享技能列表
- `Enabled`：每个已导入 skill 的开关，默认开启

Skill 行为说明：

- 导入时会先校验目标 skill 文件夹，再写入 catalog。
- 开启的 skill 会作为共享能力在多个会话之间复用。
- 关闭的 skill 会保留在 catalog 中，但不会被加载，直到再次开启。
- 除了全局共享技能外，每个会话也可以拥有自己的 session 级技能。
- 删除全局 skill 只会更新共享 catalog，不会修改原始 skill 文件夹。

底层配置结构包括：

- `StoragePath`：可选的自定义存储目录
- `Imported`：已导入 skill 列表，包含 `Enabled`、`Name`、`Path`、`Description` 和 `ImportedAt`

如果没有显式配置 `StoragePath`，EventHorizon 会使用用户主目录下的默认 skill 存储位置。

### 基于 HTTP 的 MCP

EventHorizon 现在通过 HTTP 连接 MCP 服务器。

你可以在 Web UI 的 `Settings -> MCP` 中配置 MCP 服务。

每个 MCP 服务现在包含以下配置项：

- `Enabled`：开关，默认开启
- `Name`：可选的显示名称
- `HTTP endpoint URL`：MCP 服务地址，例如 `https://example.com/mcp`
- `HTTP headers`：可选请求头，例如 `Authorization=Bearer ...`

开启的 MCP 服务会自动连接并暴露给代理使用。
关闭的 MCP 服务会保留在配置中，但不会建立连接，直到再次开启。

后端使用 MCP 的 HTTP client transport，优先采用 Streamable HTTP，协议回退由 MCP 客户端库处理。

### 开发环境（本地运行）

在开始之前，请确保你的开发环境已安装以下依赖：
* [Node.js](https://nodejs.org/) (建议 v18+)
* [.NET SDK](https://dotnet.microsoft.com/download) (建议 .NET 10.0+)

后端基于 .NET 架构，请在项目根目录下执行以下命令：

```bash
dotnet run --project src/EventHorizon
```

Web UI 用于可视化与 Code Agent 的交互，可以直接在项目根目录下运行：

```
npm run dev --prefix eventhorizon-workbench
```

启动成功后，打开浏览器访问 http://localhost:5173 即可进入前端界面。

提示：首次启动后，同样需要前往 Web UI 右上角的 Settings（设置） 配置你的模型服务商（Provider）。
你也可以在同一个 Settings 对话框里配置共享的 HTTP MCP 服务。

