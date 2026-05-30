# EventHorizon

[中文](README.zh-CN.md) | English

EventHorizon is a .NET-based coding agent host with AGUI APIs, embedded web assets, conversation persistence, provider switching, MCP integration, and skill import support.

## Highlights

- Shared host for EventHorizon runtime and AGUI APIs
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
3. `--config <path>`
4. `EVENTHORIZON__...` environment variables

On first start, EventHorizon creates `~/.eventhorizon/` and seeds `~/.eventhorizon/appsettings.json` from the bundled `appsettings.json` if needed.

`CurrentProvider` is now replaced by `CurrentDefaultProvider`.
Legacy `CurrentProvider` is still read during load and migrated in memory. Persisted config only writes `CurrentDefaultProvider`.

## Run

```zsh
dotnet run --project src/EventHorizon -- --config samples/openai-compatible.eventhorizon.json
```

By default the server listens on the URLs configured in `AGUI:Urls` and serves:

- `api/*` for controllers
- embedded static assets for the AGUI workbench shell
- `/agui` for the raw AGUI endpoint when runtime initialization succeeds

If no valid provider is configured yet, the app still starts so configuration APIs and UI can be used first.

## Conversations

- Each conversation stores its own `ProviderName` and `Model`
- If a conversation has no explicit provider, `CurrentDefaultProvider` is used
- If a conversation has no explicit model, the selected provider default model is used
- Switching provider/model updates only that conversation and preserves transcript, title, summary, and other session state
- Agent/runtime objects are cached per conversation and rebuilt only when that conversation changes or is invalidated

## AGUI DTOs

AGUI boundary DTOs live under `src/EventHorizon/AGUI/DTOs/`.
Type names end with `DTO`, for example:

- `CreateAGUISessionRequestDTO`
- `UpdateConversationModelRequestDTO`
- `ConversationModelResponseDTO`
- `AGUISessionSummaryDTO`
- `AGUIRunDTO`

## MCP And Skills

- MCP servers are configured in app settings and can be tested through `api/mcp/test`
- Skills are validated before import and copied into `~/.eventhorizon/skills`
- Imported skills are tracked in the persisted skill catalog

## Samples

See `samples/README.md` and the sample files in `samples/` for:

- default provider config
- multi-provider config
- external `--config` usage
- MCP examples
- skill folder examples

## Development

```zsh
dotnet format EventHorizon.slnx
dotnet build EventHorizon.slnx
dotnet test EventHorizon.slnx
```
