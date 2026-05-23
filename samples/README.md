# Samples

This directory contains sample configuration files for all supported provider types.

## Available Samples

| File | Provider Type | Description |
|------|--------------|-------------|
| `openai-api-key.eventhorizon.json` | `openai` | Official OpenAI API |
| `openai-compatible.eventhorizon.json` | `openai-compatible` | Custom OpenAI-compatible endpoint (proxy, gateway, Ollama) |
| `azure-openai.eventhorizon.json` | `azure-openai` | Azure OpenAI Service |
| `anthropic.eventhorizon.json` | `anthropic` | Anthropic Claude API |
| `gemini.eventhorizon.json` | `gemini` | Google Gemini API |

## Quick Start with TUI

Run the interactive terminal workbench with any sample config:

```zsh
dotnet run --project src/EventHorizon -- tui --config samples/openai-compatible.eventhorizon.json
```

## Sample Configurations

### OpenAI API

```zsh
dotnet run --project src/EventHorizon -- tui --config samples/openai-api-key.eventhorizon.json
```

### OpenAI-Compatible

```zsh
dotnet run --project src/EventHorizon -- tui --config samples/openai-compatible.eventhorizon.json
```

### Azure OpenAI

```zsh
dotnet run --project src/EventHorizon -- tui --config samples/azure-openai.eventhorizon.json
```

### Anthropic Claude

```zsh
dotnet run --project src/EventHorizon -- tui --config samples/anthropic.eventhorizon.json
```

### Google Gemini

```zsh
dotnet run --project src/EventHorizon -- tui --config samples/gemini.eventhorizon.json
```

## Configuration Reference

### Provider Options

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `Type` | string | ✓ | `openai`, `azure-openai`, `anthropic`, `gemini`, or `openai-compatible` |
| `Model` | string | ✓ | Model name |
| `ApiKey` | string | ✓ for openai/anthropic/gemini | API key |
| `Endpoint` | string | ✓ for azure-openai/openai-compatible | Endpoint URL |
| `Deployment` | string | ✓ for azure-openai | Azure deployment name |
| `UseDefaultAzureCredential` | bool | - | Use Azure AD credentials |
