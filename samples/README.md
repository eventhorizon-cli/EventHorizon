# Samples

These samples match the current shared-host AGUI server startup model.

## Run With External Config

```zsh
dotnet run --project src/EventHorizon
```

## Notes

- Use `CurrentDefaultProvider`, not `CurrentProvider`
- Conversation-level provider and model selections are persisted separately from app config
- UI/API configuration changes are written to `~/.eventhorizon/appsettings.json`

## Included Samples

- `openai-api-key.eventhorizon.json`
- `openai-compatible.eventhorizon.json`
- `azure-openai.eventhorizon.json`
- `anthropic.eventhorizon.json`
- `gemini.eventhorizon.json`

## Multi-Provider Example

```json
{
  "CurrentDefaultProvider": "openai",
  "Providers": {
    "openai": {
      "Type": "openai",
      "Model": "gpt-4.1-mini",
      "ApiKey": "sk-..."
    },
    "anthropic": {
      "Type": "anthropic",
      "Model": "claude-sonnet-4-20250514",
      "ApiKey": "sk-ant-..."
    }
  }
}
```

## MCP Example

```json
{
  "McpServers": [
    {
      "Name": "filesystem",
      "Command": "npx",
      "Arguments": ["-y", "@modelcontextprotocol/server-filesystem", "."],
      "Enabled": true
    }
  ]
}
```

## Skill Folder Example

```text
my-skill/
  SKILL.md
  prompt.txt
```

`SKILL.md` is required for import validation.
