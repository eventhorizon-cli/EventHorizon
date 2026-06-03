import { apiRequest } from "@/api/client";
import { normalizeProviderType } from "@/utils/configuration";
import type {
  AppConfiguration,
  ImportedSkill,
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
  apiKey?: string;
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

function mapProviderType(type?: string): ProviderType | undefined {
  return normalizeProviderType(type);
}

function mapMcpServer(server: McpServerConfig): McpServerConfig {
  return {
    enabled: server.enabled ?? true,
    name: server.name,
    url: server.url,
    headers: server.headers ?? {},
  };
}

function mapImportedSkill(skill: ImportedSkill): ImportedSkill {
  return {
    enabled: skill.enabled ?? true,
    name: skill.name,
    path: skill.path,
    description: skill.description,
    importedAt: skill.importedAt,
  };
}

function mapConfiguration(payload: ConfigurationPayload): AppConfiguration {
  const providers = payload.providers
    .map((provider) => ({
      payload: provider,
      type: mapProviderType(provider.type),
    }))
    .filter((provider) => provider.type)
    .map(({ payload, type }) => ({
      name: payload.name,
      provider: {
        type,
        model: payload.model,
        models: payload.models ?? [],
        endpoint: payload.endpoint,
        apiKey: payload.apiKey,
        deployment: payload.deployment,
        useDefaultAzureCredential: payload.useDefaultAzureCredential ?? false,
      },
    }));

  const currentDefaultProvider = providers.some((provider) => provider.name === payload.currentDefaultProvider)
    ? payload.currentDefaultProvider
    : undefined;

  return {
    filePath: payload.filePath,
    currentDefaultProvider,
    providers,
    mcpServers: (payload.mcpServers ?? []).map(mapMcpServer),
    skills: {
      storagePath: payload.skills?.storagePath,
      imported: (payload.skills?.imported ?? []).map(mapImportedSkill),
    },
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
  return importSkillToTarget({ path, target: "global" });
}

export async function importSkillToTarget(input: {
  path: string;
  target: "global" | "session";
  sessionId?: string;
}): Promise<SkillImportResult> {
  return apiRequest<SkillImportResult>("/api/skills/import", {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export async function removeGlobalSkill(skillName: string): Promise<{ success: boolean; message: string; errors: string[] }> {
  return apiRequest(`/api/skills/global/${encodeURIComponent(skillName)}`, {
    method: "DELETE",
  });
}

export async function removeSessionSkill(sessionId: string, skillName: string): Promise<{ success: boolean; message: string; errors: string[] }> {
  return apiRequest(`/api/skills/sessions/${encodeURIComponent(sessionId)}/${encodeURIComponent(skillName)}`, {
    method: "DELETE",
  });
}
