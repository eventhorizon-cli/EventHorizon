# EventHorizon

EventHorizon 是一个基于 **Microsoft Agent Framework** 开发的 Code Agent 项目。

---

## 🚀 快速启动

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

注意：第一次启动后需要到 Web UI 右上角的 Setting 里配置 Provider
