import { apiRequest } from "@/api/client";
import type {
  AppConfiguration,
  McpServerConfig,
  McpTestResult,
  ProviderConfig,
  ProviderEntry,
  ProviderType,
  ProviderTestResult,
  SkillCatalog,
  SkillImportResult,
} from "@/types";

type ApiProviderPayload = {
  name: string;
  type: string;
  model?: string;
  models?: string[];
  endpoint?: string;
  apiKeyMasked?: string;
  deployment?: string;
  useDefaultAzureCredential?: boolean;
};

type ConfigurationPayload = {
  filePath: string;
  currentDefaultProvider?: string;
  providers: ApiProviderPayload[];
  mcpServers: McpServerConfig[];
  skills: SkillCatalog;
};

const providerTypes: ProviderType[] = ["openai", "openai-compatible", "azure-openai", "anthropic", "gemini"];

function mapProviderType(type?: string): ProviderType | undefined {
  if (!type) {
    return undefined;
  }

  return providerTypes.find((value) => value === type);
}

function mapProvider(payload: ApiProviderPayload): ProviderEntry {
  return {
    name: payload.name,
    provider: {
      type: mapProviderType(payload.type),
      model: payload.model,
      models: payload.models ?? [],
      endpoint: payload.endpoint,
      apiKeyMasked: payload.apiKeyMasked,
      deployment: payload.deployment,
      useDefaultAzureCredential: payload.useDefaultAzureCredential ?? false,
    },
  };
}

function mapConfiguration(payload: ConfigurationPayload): AppConfiguration {
  return {
    filePath: payload.filePath,
    currentDefaultProvider: payload.currentDefaultProvider,
    providers: payload.providers.map(mapProvider),
    mcpServers: payload.mcpServers ?? [],
    skills: payload.skills ?? { imported: [] },
  };
}

export async function getConfiguration(): Promise<AppConfiguration> {
  return mapConfiguration(await apiRequest<ConfigurationPayload>("/api/configuration"));
}

export async function saveConfiguration(input: {
  currentDefaultProvider?: string;
  providers: ProviderEntry[];
  mcpServers: McpServerConfig[];
  skills: SkillCatalog;
}): Promise<AppConfiguration> {
  return mapConfiguration(
    await apiRequest<ConfigurationPayload>("/api/configuration", {
      method: "PUT",
      body: JSON.stringify({
        currentDefaultProvider: input.currentDefaultProvider,
        providers: input.providers.map((entry) => ({
          name: entry.name,
          provider: {
            type: entry.provider.type,
            model: entry.provider.model,
            models: entry.provider.models,
            endpoint: entry.provider.endpoint,
            apiKey: entry.provider.apiKey,
            deployment: entry.provider.deployment,
            useDefaultAzureCredential: entry.provider.useDefaultAzureCredential,
          },
        })),
        mcpServers: input.mcpServers,
        skills: input.skills,
      }),
    }),
  );
}

export async function testProvider(name: string, provider: ProviderConfig): Promise<ProviderTestResult> {
  return apiRequest<ProviderTestResult>("/api/providers/test", {
    method: "POST",
    body: JSON.stringify({
      name,
      provider: {
        type: provider.type,
        model: provider.model,
        models: provider.models,
        endpoint: provider.endpoint,
        apiKey: provider.apiKey,
        deployment: provider.deployment,
        useDefaultAzureCredential: provider.useDefaultAzureCredential,
      },
    }),
  });
}

export async function testMcp(server: McpServerConfig): Promise<McpTestResult> {
  return apiRequest<McpTestResult>("/api/mcp/test", {
    method: "POST",
    body: JSON.stringify({ server }),
  });
}

export async function importSkill(path: string): Promise<SkillImportResult> {
  return apiRequest<SkillImportResult>("/api/skills/import", {
    method: "POST",
    body: JSON.stringify({ path }),
  });
}
