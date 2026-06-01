import type { AppConfiguration, ProviderEntry, ProviderType } from "@/types";

export const providerTypes: ProviderType[] = ["openai", "openai-compatible", "azure-openai", "anthropic", "gemini"];

export const globalSettingsTabs = ["providers", "mcp", "skills"] as const;

export type GlobalSettingsTab = (typeof globalSettingsTabs)[number];

export function cloneProviderEntry(entry: ProviderEntry): ProviderEntry {
  return {
    name: entry.name,
    provider: {
      type: entry.provider.type,
      model: entry.provider.model,
      models: [...entry.provider.models],
      endpoint: entry.provider.endpoint,
      apiKey: entry.provider.apiKey,
      deployment: entry.provider.deployment,
      useDefaultAzureCredential: entry.provider.useDefaultAzureCredential,
    },
  };
}

export function cloneConfiguration(configuration: AppConfiguration): AppConfiguration {
  return {
    filePath: configuration.filePath,
    currentDefaultProvider: configuration.currentDefaultProvider,
    providers: configuration.providers.map(cloneProviderEntry),
    mcpServers: configuration.mcpServers.map((server) => ({
      ...server,
      enabled: server.enabled,
      headers: { ...server.headers },
    })),
    skills: {
      storagePath: configuration.skills.storagePath,
      imported: configuration.skills.imported.map((skill) => ({ ...skill, enabled: skill.enabled })),
    },
  };
}

export function createProviderDraft(): ProviderEntry {
  return {
    name: "",
    provider: {
      type: undefined,
      model: undefined,
      models: [],
      endpoint: undefined,
      apiKey: undefined,
      deployment: undefined,
      useDefaultAzureCredential: false,
    },
  };
}

export function getProvider(configuration: AppConfiguration | undefined, providerName?: string) {
  if (!configuration || !providerName) {
    return undefined;
  }

  return configuration.providers.find((provider) => provider.name === providerName);
}

export function getProviderModels(provider: ProviderEntry | undefined, currentModel?: string) {
  const models = [...(provider?.provider.models ?? [])];

  if (provider?.provider.model && !models.includes(provider.provider.model)) {
    models.unshift(provider.provider.model);
  }

  if (currentModel && !models.includes(currentModel)) {
    models.unshift(currentModel);
  }

  return [...new Set(models.filter(Boolean))];
}

export function normalizeOptionalText(value: string) {
  const trimmed = value.trim();
  return trimmed ? trimmed : undefined;
}

export function isProviderFieldVisible(
  providerType: ProviderType | undefined,
  field: "model" | "endpoint" | "apiKey" | "deployment" | "useDefaultAzureCredential",
) {
  switch (providerType) {
    case "openai":
    case "anthropic":
    case "gemini":
      return field === "model" || field === "apiKey";
    case "openai-compatible":
      return field === "model" || field === "endpoint" || field === "apiKey";
    case "azure-openai":
      return field === "model" || field === "endpoint" || field === "apiKey" || field === "deployment" || field === "useDefaultAzureCredential";
    default:
      return false;
  }
}

export function getProviderFieldMeta(
  providerType: ProviderType | undefined,
  field: "model" | "endpoint" | "apiKey" | "deployment",
) {
  if (field === "apiKey") {
    if (providerType === "azure-openai") {
      return { label: "API key (optional)", hint: "Leave empty to use Default Azure Credential." };
    }

    if (providerType === "openai-compatible") {
      return { label: "API key (optional)", hint: "Optional for compatible endpoints that do not require authentication." };
    }

    return { label: "API key", hint: "Required" };
  }

  if (field === "deployment") {
    return { label: "Deployment", hint: "Required for Azure OpenAI unless you reuse the model name." };
  }

  if (field === "endpoint") {
    return { label: "Endpoint", hint: "Required" };
  }

  return {
    label: providerType === "azure-openai" ? "Default model (optional)" : "Default model",
    hint: providerType === "azure-openai" ? "Optional when deployment is set explicitly." : "Required",
  };
}
