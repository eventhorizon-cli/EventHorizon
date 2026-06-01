# EventHorizon

[中文](README.zh-CN.md) | English

EventHorizon is a .NET-based coding agent host with Engine APIs, embedded Workbench assets, conversation persistence, provider switching, MCP integration, and skill import support.

## Highlights

- Shared host for EventHorizon runtime and Engine APIs
- Per-conversation provider and model persistence
- Conversation-level agent cache and rebuild-on-change behavior
- Provider configuration persisted to `~/.eventhorizon/appsettings.json`
- MCP server configuration and connection testing
- Skill folder validation and import into `~/.eventhorizon/skills`
- Conversation deletion and AI-generated conversation titles

## Configuration Priority

Configuration is loaded in this order, with later sources overriding earlier ones:

1. Built-in `src/EventHorizon/appsettings.json`
2. `~/.eventhorizon/appsettings.json`

On first start, EventHorizon creates `~/.eventhorizon/` and seeds `~/.eventhorizon/appsettings.json` from the bundled `appsettings.json` if needed.

## Run

```zsh
dotnet run --project src/EventHorizon
```

By default the server listens on the `Kestrel:Endpoints:EventHorizon:Url` setting in `src/EventHorizon/appsettings.json` and serves:

- `api/*` for controllers
- embedded static assets for the Workbench shell
- `/api/sessions/{sessionId}/runs/{runId}/events` for run streaming

If no valid provider is configured yet, the app still starts so configuration APIs and UI can be used first.

## Conversations

- Each conversation stores its own `ProviderName` and `Model`
- If a conversation has no explicit provider, `CurrentDefaultProvider` is used
- If a conversation has no explicit model, the selected provider default model is used
- Switching provider/model updates only that conversation and preserves transcript, title, summary, and other session state
- Agent/runtime objects are cached per conversation and rebuilt only when that conversation changes or is invalidated

## MCP And Skills

- MCP servers are configured in app settings and can be tested through `api/mcp/test`
- Skills are validated before import and copied into `~/.eventhorizon/skills`
- Imported skills are tracked in the persisted skill catalog

## Samples

See `samples/README.md` and the sample files in `samples/` for:

- default provider config
- multi-provider config
- MCP examples
- skill folder examples

## Development

```zsh
dotnet format EventHorizon.slnx
dotnet build EventHorizon.slnx
dotnet test EventHorizon.slnx
```
