import type { AppConfiguration, ProviderApiType, ProviderEntry, ProviderFamily, ProviderType } from "@/types";

export const providerTypes: ProviderType[] = [
  "openai-chat-completions",
  "openai-responses",
  "openai-compatible-chat-completions",
  "openai-compatible-responses",
  "azure-openai-chat-completions",
  "azure-openai-responses",
  "anthropic",
  "gemini",
];

export const providerFamilies: ProviderFamily[] = ["openai", "openai-compatible", "azure-openai", "anthropic", "gemini"];

export const providerApiTypes: ProviderApiType[] = ["chat", "responses"];

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

export function isSupportedProviderType(type?: string): type is ProviderType {
  return !!type && providerTypes.includes(type as ProviderType);
}

export function normalizeProviderType(type?: string): ProviderType | undefined {
  const normalized = normalizeOptionalText(type ?? "")?.toLowerCase();
  return isSupportedProviderType(normalized) ? normalized : undefined;
}

export function getProviderFamily(providerType?: ProviderType | string): ProviderFamily | undefined {
  switch (normalizeProviderType(providerType)) {
    case "openai-chat-completions":
    case "openai-responses":
      return "openai";
    case "openai-compatible-chat-completions":
    case "openai-compatible-responses":
      return "openai-compatible";
    case "azure-openai-chat-completions":
    case "azure-openai-responses":
      return "azure-openai";
    case "anthropic":
      return "anthropic";
    case "gemini":
      return "gemini";
    default:
      return undefined;
  }
}

export function getProviderApiType(providerType?: ProviderType | string): ProviderApiType | undefined {
  switch (normalizeProviderType(providerType)) {
    case "openai-chat-completions":
    case "openai-compatible-chat-completions":
    case "azure-openai-chat-completions":
      return "chat";
    case "openai-responses":
    case "openai-compatible-responses":
    case "azure-openai-responses":
      return "responses";
    default:
      return undefined;
  }
}

export function supportsProviderApiType(providerFamily?: ProviderFamily | string) {
  return providerFamily === "openai" || providerFamily === "openai-compatible" || providerFamily === "azure-openai";
}

export function toProviderType(providerFamily?: ProviderFamily, apiType: ProviderApiType = "chat"): ProviderType | undefined {
  switch (providerFamily) {
    case "openai":
      return apiType === "responses" ? "openai-responses" : "openai-chat-completions";
    case "openai-compatible":
      return apiType === "responses" ? "openai-compatible-responses" : "openai-compatible-chat-completions";
    case "azure-openai":
      return apiType === "responses" ? "azure-openai-responses" : "azure-openai-chat-completions";
    case "anthropic":
      return "anthropic";
    case "gemini":
      return "gemini";
    default:
      return undefined;
  }
}

export function getProviderFamilyLabel(providerFamily: ProviderFamily) {
  switch (providerFamily) {
    case "openai":
      return "OpenAI";
    case "openai-compatible":
      return "OpenAI Compatible";
    case "azure-openai":
      return "Azure OpenAI";
    case "anthropic":
      return "Anthropic";
    case "gemini":
      return "Gemini";
  }
}

export function getProviderApiTypeLabel(providerApiType: ProviderApiType) {
  return providerApiType === "responses" ? "Responses API" : "Chat Completions API";
}

export function formatProviderTypeLabel(providerType?: ProviderType | string) {
  const normalizedProviderType = normalizeProviderType(providerType);

  if (!normalizedProviderType) {
    return providerType ?? "Unknown";
  }

  const providerFamily = getProviderFamily(normalizedProviderType);
  if (!providerFamily) {
    return normalizedProviderType;
  }

  if (!supportsProviderApiType(providerFamily)) {
    return getProviderFamilyLabel(providerFamily);
  }

  const apiType = getProviderApiType(normalizedProviderType);
  return apiType
    ? `${getProviderFamilyLabel(providerFamily)} / ${getProviderApiTypeLabel(apiType)}`
    : getProviderFamilyLabel(providerFamily);
}

export function isProviderFieldVisible(
  providerType: ProviderType | undefined,
  field: "model" | "endpoint" | "apiKey" | "deployment" | "useDefaultAzureCredential",
) {
  switch (getProviderFamily(providerType)) {
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
  const providerFamily = getProviderFamily(providerType);

  if (field === "apiKey") {
    if (providerFamily === "azure-openai") {
      return { label: "API key (optional)", hint: "Leave empty to use Default Azure Credential." };
    }

    if (providerFamily === "openai-compatible") {
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
    label: providerFamily === "azure-openai" ? "Default model (optional)" : "Default model",
    hint: providerFamily === "azure-openai" ? "Optional when deployment is set explicitly." : "Required",
  };
}
