# EventHorizon

EventHorizon is now a **terminal-first coding agent runtime** built around a simpler architecture:

- a `ParserConfiguration`-based CLI layer built with `System.CommandLine`
- a runtime bootstrap that snapshots workspace context and tool metadata up front
- a small query engine that owns session state and chat history, plus a dedicated turn loop for streaming execution
- an interactive coding workbench for `chat` / `tui`, with a progressive launchpad first screen before expanding into explorer, transcript, activity, command palette, and inspector panels
- a console host with a centralized slash-command registry for `/help`, `/tools`, `/context`, `/history`, `/reset`, and `/exit`
- provider-backed execution through the existing OpenAI / Azure OpenAI / Anthropic / Gemini integrations
- AGUI server/client and MCP server entrypoints retained as first-class launch modes

## Architecture

The main runtime lives in `src/EventHorizon/` and is now organized around these concepts:

- `Cli/` — command parsing using `System.CommandLine` and `ParserConfiguration`
- `EntryPoints/` — application entrypoints and console host orchestration
- `Commands/` — slash command registration and execution
- `Context/` — session context snapshotting, including workspace summary and git snapshot
- `Prompting/` — prompt assembly from context + tools + runtime guidance
- `Execution/` — query engine state, query loop execution, and conversation models
- `Tools/` — tool contracts and registry metadata
- `Providers/` — model/runtime factory integration
- `Workspace/` — local filesystem and shell capabilities

## Commands

`EventHorizon` supports these commands:

- `tui` — interactive multi-panel workbench (recommended)
- `chat` — alias for `tui`
- `run <prompt...>` — single prompt execution
- `serve` — host the AGUI endpoint
- `client` — connect to a remote AGUI endpoint
- `mcp-server` — expose the runtime as an MCP tool over stdio
- `prices-refresh` — refresh the cached pricing catalog

If no command is specified and the input begins with options, the CLI defaults to `tui`.
If the first token is not a known command, the CLI treats the input as a `run` prompt.

## Configuration

Configuration flows through `AppOptionsLoader` and supports:

1. built-in `appsettings.json`
2. local `appsettings.json`
3. local `eventhorizon.json`
4. `~/.config/eventhorizon/appsettings.json`
5. `~/.config/eventhorizon/eventhorizon.json`
6. `--config <path>`
7. `EVENTHORIZON__...` environment variables
8. provider-specific environment fallbacks

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
  "WorkspaceRoot": ".",
  "Agent": {
    "Name": "EventHorizon",
    "Description": "A terminal-first coding agent runtime.",
    "EnableSkills": true,
    "EnableShell": true,
    "EnableMcpTools": true,
    "AdditionalSystemPrompts": []
  },
  "Provider": {
    "Type": "openai",
    "Model": "gpt-4.1-mini",
    "ApiKey": null,
    "Endpoint": null,
    "Deployment": null,
    "UseDefaultAzureCredential": true
  },
  "Protocol": {
    "Url": "http://127.0.0.1:8787",
    "Path": "/agui",
    "ClientUrl": "http://127.0.0.1:8787/agui"
  },
  "Pricing": {
    "CatalogUrl": "https://raw.githubusercontent.com/BerriAI/litellm/main/model_prices_and_context_window.json",
    "CachePath": null,
    "RefreshOnStartup": true
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

## Quick start

Build:

```zsh
dotnet build EventHorizon.slnx
```

Refresh the pricing catalog:

```zsh
dotnet run --project src/EventHorizon -- prices-refresh
```

**Recommended:** Run interactive TUI mode (multi-panel workbench):

```zsh
dotnet run --project src/EventHorizon -- tui
```

The `tui` mode opens a progressive launchpad first so you can confirm the model connection before expanding into the full coding workbench with Explorer, Conversation, Activity, Command Palette, and Inspector panels. Session snapshots are autosaved by default under `.eventhorizon/sessions/` inside the configured workspace unless `Conversation.StoragePath` overrides it.

Run TUI mode with a sample config:

```zsh
dotnet run --project src/EventHorizon -- tui --config samples/openai-compatible.eventhorizon.json
```

Run TUI mode with the OpenAI API key config:

```zsh
dotnet run --project src/EventHorizon -- tui --config samples/openai-api-key.eventhorizon.json
```

Run a single prompt:

```zsh
dotnet run --project src/EventHorizon -- run "Summarize the workspace architecture"
```

Run a single prompt with a config:

```zsh
dotnet run --project src/EventHorizon -- run --config samples/openai-compatible.eventhorizon.json "Summarize the workspace architecture"
```

Start AGUI server mode:

```zsh
dotnet run --project src/EventHorizon -- serve --url http://127.0.0.1:8787
```

Connect to a remote AGUI endpoint:

```zsh
dotnet run --project src/EventHorizon -- client --url http://127.0.0.1:8787/agui
```

Expose the runtime over MCP stdio:

```zsh
dotnet run --project src/EventHorizon -- mcp-server
```

## Verified commands

The following commands were verified during this rewrite:

```zsh
dotnet build EventHorizon.slnx
dotnet test EventHorizon.slnx
dotnet run --project src/EventHorizon -- prices-refresh
```
