EventHorizon
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

### Provider configuration

Configure providers from the Web UI under `Settings -> Providers`.

The application supports these provider types:

- `openai`
- `openai-compatible`
- `azure-openai`
- `anthropic`
- `gemini`

Each provider entry includes:

- `Name`: unique provider name shown in the UI and used for default selection
- `Type`: provider type
- `Default model`: the model used when a session does not override it
- `Available models`: optional list of selectable models shown in the UI, one model ID per line

Additional fields depend on the provider type:

- `API key`: required for most providers
- `Endpoint`: required for `openai-compatible` and `azure-openai`
- `Deployment`: supported by `azure-openai`
- `Use default Azure credential`: supported by `azure-openai`; useful when you do not want to store an API key locally

Provider behavior notes:

- You can configure multiple providers and choose one as the shared default.
- Sessions can override the global provider and model selection.
- For Azure OpenAI, EventHorizon prefers `Deployment` and falls back to `Model` when needed.
- For `openai-compatible`, use the provider's base endpoint and set the model IDs exposed by that service.

Example provider settings:

- OpenAI
  - Type: `openai`
  - API key: your OpenAI API key
  - Default model: `gpt-4.1-mini`
- Azure OpenAI
  - Type: `azure-openai`
  - Endpoint: your Azure OpenAI resource endpoint
  - Deployment: your deployment name
  - Default model: optional
  - API key: optional when `Use default Azure credential` is enabled
- Anthropic
  - Type: `anthropic`
  - API key: your Anthropic API key
  - Default model: `claude-sonnet-4-0`

### Skill configuration

Configure shared skills from the Web UI under `Settings -> Skills`.

Skills are imported from local folders and stored in a shared skill catalog.

The global skill settings include:

- `Skill import path`: a local folder path to import
- `Storage path`: the folder used by EventHorizon to store imported skill metadata
- `Imported skills`: the shared list of currently imported skills
- `Enabled`: on/off toggle for each imported skill, enabled by default

Skill behavior notes:

- Import validates the target skill folder before saving it into the catalog.
- Enabled imported skills are available as shared capabilities across conversations.
- Disabled imported skills remain in the catalog but are not loaded until they are turned on again.
- A session can also have its own session-scoped skills in addition to shared global skills.
- Removing a global skill updates the shared catalog but does not modify the original source folder.

The backing configuration stores skill data as:

- `StoragePath`: optional custom storage directory
- `Imported`: list of imported skills with `Enabled`, `Name`, `Path`, `Description`, and `ImportedAt`

If `StoragePath` is not configured, EventHorizon uses its default skill storage location under the user home directory.

### MCP over HTTP

EventHorizon now connects to MCP servers over HTTP.

Configure MCP servers from the Web UI under `Settings -> MCP`.

Each MCP server now uses:

- `Enabled`: on/off toggle, enabled by default
- `Name`: optional display name
- `HTTP endpoint URL`: the MCP server endpoint, for example `https://example.com/mcp`
- `HTTP headers`: optional request headers such as `Authorization=Bearer ...`

Enabled MCP servers are connected automatically and exposed to the agent.
Disabled MCP servers remain in the configuration but are not connected until they are turned on again.

The backend uses the MCP HTTP client transport and prefers Streamable HTTP, with protocol fallback handled by the MCP client library.

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
You can also configure shared HTTP MCP servers from the same Settings dialog.
