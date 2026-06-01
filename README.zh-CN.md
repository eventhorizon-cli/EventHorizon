# EventHorizon

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

