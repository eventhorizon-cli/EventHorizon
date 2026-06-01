# EventHorizon

===========

[![Nuget](https://img.shields.io/nuget/v/EventHorizon)](https://www.nuget.org/packages/EventHorizon/)

English | [简体中文](./README.zh-CN.md)

EventHorizon is a Code Agent project developed based on the **Microsoft Agent Framework**.

---

## Quick Start

### Release Version

If you just want to use the published version of EventHorizon, you can quickly install it globally via the .NET Core CLI:

```
dotnet tool install --global EventHorizon
```

After installation, start EventHorizon with: eventhorizon

http://localhost:9527

You should see the Web UI

Note: After launching for the first time, you need to go to Settings in the upper right corner of the Web UI to configure your Provider.

### Development (Local Run)

Before you begin, please ensure that your development environment has the following dependencies installed:
* [Node.js](https://nodejs.org/) (v18+ recommended)
* [.NET SDK](https://dotnet.microsoft.com/download) (.NET 10.0+ recommended)

The backend is built on the .NET architecture. Please execute the following command in the project root directory:

```bash
dotnet run --project src/EventHorizon
```

The Web UI is used to visualize interactions with the Code Agent and can be run directly from the project root directory:

```bash
npm run dev --prefix eventhorizon-workbench
```
Once started, you can access the interface by opening http://localhost:5173 in your browser.

Note: After launching for the first time, you need to go to Settings in the upper right corner of the Web UI to configure your Provider.
