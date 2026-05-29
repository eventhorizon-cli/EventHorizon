# EventHorizon

EventHorizon is an **AGUI-first coding agent runtime** with a .NET backend and a Vite/React workbench frontend.

- the backend hosts sessions, runs, event streaming, file tools, and the AGUI API
- the frontend workbench provides chat, files, logs, and side-by-side diff views
- file diff data is tracked by EventHorizon itself per run and does not depend on Git as the primary source
- provider-backed execution continues to support OpenAI / Azure OpenAI / Anthropic / Gemini integrations
- the published `.NET tool` serves the Web UI from embedded static assets

## Architecture

The main runtime lives in `src/EventHorizon/` and is organized around these concepts:

- `AGUI/` — HTTP API endpoints, session/run management, and event streaming
- `Diff/` — non-Git file snapshots, file state tracking, and diff generation per run
- `EntryPoints/` — application startup and AGUI server orchestration
- `Providers/` — model/runtime factory integration
- `Tools/` — tool contracts and registry metadata
- `Workspace/` — local filesystem and shell capabilities used by the agent

The frontend workbench lives in `eventhorizon-workbench/` and builds to `eventhorizon-workbench/dist`.

## Startup

`EventHorizon` currently starts the AGUI server and workbench experience.

```zsh
dotnet run --project src/EventHorizon -- --config samples/openai-compatible.eventhorizon.json
```

The server exposes `/api/*` for backend APIs and serves the workbench UI for non-API routes.

## Configuration

Configuration flows through `AppOptionsLoader` and supports:

1. built-in `appsettings.json`
2. `~/.config/eventhorizon.json` (created automatically if missing)
3. local `appsettings.json`
4. local `eventhorizon.json`
5. `--config <path>`
6. `EVENTHORIZON__...` environment variables
7. provider-specific environment fallbacks

Later providers override earlier ones using the standard .NET configuration system, so you can keep defaults in your home config and override them per project or per command.

`WorkspaceRoot` is no longer stored in configuration. By default EventHorizon uses the directory where you open/run the project, and you can override it for a single run with `--workspace <path>`.

### Provider Types

EventHorizon supports the following provider types:

| Provider Type | Description | Required Options | Environment Variables |
|--------------|-------------|------------------|---------------------|
| `openai` | Official OpenAI API | `ApiKey`, `Model` | `OPENAI_API_KEY`, `OPENAI_CHAT_MODEL_NAME` |
| `azure-openai` | Azure OpenAI Service | `Endpoint`, `Deployment` or `Model` | `AZURE_OPENAI_ENDPOINT`, `AZURE_OPENAI_DEPLOYMENT_NAME`, `AZURE_OPENAI_API_KEY` |
| `anthropic` | Anthropic Claude API | `ApiKey`, `Model` | `ANTHROPIC_API_KEY`, `ANTHROPIC_CHAT_MODEL_NAME` |
| `gemini` | Google Gemini API | `ApiKey`, `Model` | `GOOGLE_GENAI_API_KEY`, `GOOGLE_GENAI_MODEL_NAME` |
| `openai-compatible` | Custom OpenAI-compatible endpoint | `Endpoint`, `Model` | Custom API key via `ApiKey` |

### Configuration Options

```json
{
  "Agent": {
    "Name": "EventHorizon",
    "Description": "A terminal-first coding agent runtime.",
    "EnableSkills": true,
    "EnableShell": true,
    "EnableMcpTools": true,
    "AdditionalSystemPrompts": []
  },
  "CurrentProvider": "openai",
  "Providers": {
    "openai": {
      "Type": "openai",
      "Model": "gpt-4.1-mini",
      "ApiKey": null,
      "Endpoint": null,
      "Deployment": null,
      "UseDefaultAzureCredential": true
    },
    "azure": {
      "Type": "azure-openai",
      "Model": "gpt-4o-mini",
      "ApiKey": null,
      "Endpoint": "https://example.openai.azure.com/",
      "Deployment": "gpt-4o-mini",
      "UseDefaultAzureCredential": true
    }
  },
  "Protocol": {
    "Url": "http://127.0.0.1:8787",
    "Path": "/agui",
    "ClientUrl": "http://127.0.0.1:8787/agui"
  },
  "Pricing": {
    "CatalogUrl": "https://raw.githubusercontent.com/BerriAI/litellm/main/model_prices_and_context_window.json",
    "CachePath": null,
    "RefreshOnStartup": false
  },
  "Conversation": {
    "StoragePath": null,
    "AutoSave": true,
    "AutoSaveSessionName": "last-session"
  },
  "McpServers": [
    {
      "Name": "filesystem",
      "Command": "npx",
      "Arguments": ["-y", "@modelcontextprotocol/server-filesystem", "."],
      "Enabled": false
    }
  ]
}
```

Sample configs live under `samples/` with configurations for all supported provider types.

If multiple named providers are configured and `CurrentProvider` is missing, EventHorizon will prompt you to choose one on startup and persist your selection back to `~/.config/eventhorizon.json`.

## Frontend development

Local frontend development uses the Vite dev server and does **not** write directly to `src/EventHorizon/wwwroot`.

```zsh
cd eventhorizon-workbench
npm install
npm run dev
```

Local frontend production builds emit to `eventhorizon-workbench/dist`.

```zsh
cd eventhorizon-workbench
npm run build
```

## Build and package

Build the solution locally:

```zsh
dotnet build EventHorizon.slnx
```

The CI/package flow is:

1. build `eventhorizon-workbench/dist`
2. copy `dist` into `src/EventHorizon/wwwroot`
3. build/test the .NET solution
4. pack `src/EventHorizon.Tool/EventHorizon.Tool.csproj`

`src/EventHorizon/wwwroot` is committed as a directory marker via `.gitkeep`, but local Vite builds still target `eventhorizon-workbench/dist`.

During build/pack, files copied into `src/EventHorizon/wwwroot` are embedded into the `EventHorizon` assembly. At runtime, the tool serves the workbench UI from embedded resources, so the installed tool does not depend on a physical `wwwroot` directory.

