import { useState } from "react";
import { X } from "lucide-react";
import { ToggleSwitch } from "@/components/settings/ToggleSwitch";
import { cn } from "@/utils/cn";
import {
  getProviderApiType,
  getProviderApiTypeLabel,
  getProviderFamily,
  getProviderFamilyLabel,
  getProviderFieldMeta,
  globalSettingsTabs,
  isProviderFieldVisible,
  providerApiTypes,
  providerFamilies,
  supportsProviderApiType,
  toProviderType,
} from "@/utils/configuration";
import type {
  AppConfiguration,
  ImportedSkill,
  McpServerConfig,
  ProviderApiType,
  ProviderEntry,
  ProviderFamily,
} from "@/types";
import type { GlobalSettingsTab } from "@/utils/configuration";

type GlobalSettingsDialogProps = {
  open: boolean;
  configuration?: AppConfiguration;
  configurationDraft?: AppConfiguration;
  configurationError?: string;
  globalSettingsTab: GlobalSettingsTab;
  globalSettingsMessage?: string;
  globalSettingsError?: string;
  isLoadingConfiguration: boolean;
  isSavingConfiguration: boolean;
  isImportingSkill: boolean;
  skillImportPath: string;
  mcpTestResults: Record<number, string>;
  providerTestResults: Record<string, string>;
  testingProviderIndexes: Record<string, boolean>;
  onClose: () => void;
  onTabChange: (tab: GlobalSettingsTab) => void;
  onRefreshConfiguration: () => Promise<void> | void;
  onSaveConfiguration: () => Promise<void> | void;
  onConfigurationDraftChange: (configuration: AppConfiguration) => void;
  onAddProvider: () => void;
  onRemoveProvider: (index: number) => void;
  onConfigurationFieldChange: (index: number, field: keyof ProviderEntry, value: string) => void;
  onProviderConfigChange: (index: number, field: keyof ProviderEntry["provider"], value: string | boolean) => void;
  onTestProvider: (index: number, model?: string) => Promise<void> | void;
  onAddMcpServer: () => void;
  onRemoveMcpServer: (index: number) => void;
  onMcpServerChange: (index: number, field: keyof McpServerConfig, value: string | boolean) => void;
  onTestMcpServer: (index: number) => Promise<void> | void;
  onGlobalSkillChange: (index: number, field: keyof ImportedSkill, value: string | boolean) => void;
  onSkillImportPathChange: (value: string) => void;
  onOpenSkillDirectoryPicker: () => Promise<void> | void;
  onImportSkill: () => Promise<void> | void;
  onRemoveGlobalSkill: (skillName: string) => Promise<void> | void;
};

const singleLineFieldClassName =
  "h-10 rounded-xl border border-border bg-background px-3 text-sm outline-none focus:ring-2 focus:ring-primary";

const secondaryButtonClassName =
  "inline-flex h-10 items-center justify-center rounded-xl border border-border px-3 py-2 text-xs font-medium transition hover:bg-muted";

const primaryButtonClassName =
  "inline-flex h-10 items-center justify-center rounded-xl bg-primary px-3 py-2 text-xs font-medium text-primary-foreground transition hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-50";

const compactButtonClassName =
  "inline-flex h-8 items-center justify-center rounded-xl border border-border px-2.5 py-1.5 text-xs text-muted-foreground transition hover:bg-muted disabled:cursor-not-allowed disabled:opacity-50";

const fieldLabelClassName = "text-xs font-medium text-muted-foreground uppercase";

function FieldLabel({ label, status }: { label: string; status?: "required" | "optional" }) {
  return (
    <span className={fieldLabelClassName}>
      {label}
      {status ? <span className="font-normal text-muted-foreground/80"> ({status})</span> : null}
    </span>
  );
}

export function GlobalSettingsDialog({
  open,
  configuration,
  configurationDraft,
  configurationError,
  globalSettingsTab,
  globalSettingsMessage,
  globalSettingsError,
  isLoadingConfiguration,
  isSavingConfiguration,
  isImportingSkill,
  skillImportPath,
  mcpTestResults,
  providerTestResults,
  testingProviderIndexes,
  onClose,
  onTabChange,
  onRefreshConfiguration,
  onSaveConfiguration,
  onConfigurationDraftChange,
  onAddProvider,
  onRemoveProvider,
  onConfigurationFieldChange,
  onProviderConfigChange,
  onTestProvider,
  onAddMcpServer,
  onRemoveMcpServer,
  onMcpServerChange,
  onTestMcpServer,
  onGlobalSkillChange,
  onSkillImportPathChange,
  onOpenSkillDirectoryPicker,
  onImportSkill,
  onRemoveGlobalSkill,
}: GlobalSettingsDialogProps) {
  const [newProviderModels, setNewProviderModels] = useState<Record<number, string>>({});

  if (!open) {
    return null;
  }

  function getProviderModels(provider: ProviderEntry) {
    const models = [...provider.provider.models];
    if (provider.provider.defaultModel && !models.includes(provider.provider.defaultModel)) {
      models.unshift(provider.provider.defaultModel);
    }

    return [...new Set(models.filter(Boolean))];
  }

  function getProviderTestKey(index: number, model?: string) {
    return model ? `${index}:${model}` : String(index);
  }

  function updateProviderModels(index: number, provider: ProviderEntry, models: string[], defaultModel?: string) {
    const nextModels = [...new Set(models.map((model) => model.trim()).filter(Boolean))];
    onProviderConfigChange(index, "models", nextModels.join("\n"));

    const nextDefaultModel = defaultModel && nextModels.includes(defaultModel)
      ? defaultModel
      : nextModels[0] ?? "";
    onProviderConfigChange(index, "defaultModel", nextDefaultModel);
  }

  function addProviderModel(index: number, provider: ProviderEntry) {
    const modelInput = newProviderModels[index] ?? "";
    const modelsToAdd = modelInput
      .split(/[\n,]/)
      .map((model) => model.trim())
      .filter(Boolean);

    if (modelsToAdd.length === 0) {
      return;
    }

    const models = getProviderModels(provider);
    const defaultModel = provider.provider.defaultModel || modelsToAdd[0];
    updateProviderModels(index, provider, [...models, ...modelsToAdd], defaultModel);
    setNewProviderModels((previous) => ({ ...previous, [index]: "" }));
  }

  function removeProviderModel(index: number, provider: ProviderEntry, model: string) {
    const models = getProviderModels(provider).filter((item) => item !== model);
    updateProviderModels(index, provider, models, provider.provider.defaultModel === model ? undefined : provider.provider.defaultModel);
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
        <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={onClose} />
        <div className="relative z-10 flex max-h-[85vh] w-full max-w-5xl flex-col overflow-hidden rounded-3xl border border-border bg-background shadow-xl">
          <div className="flex items-center justify-between border-b border-border px-6 py-4">
            <div>
              <h2 className="text-lg font-semibold">Global Settings</h2>
              <div className="mt-1 text-xs text-muted-foreground">Manage shared provider configuration for all conversations.</div>
            </div>
            <button
              type="button"
              onClick={onClose}
              className="rounded-xl p-2 text-muted-foreground transition hover:bg-muted hover:text-foreground"
            >
              <X className="h-5 w-5" />
            </button>
          </div>

          <div className="min-h-0 flex-1 overflow-y-auto p-6">
            <div className="space-y-4">
              <div className="flex flex-wrap items-center justify-between gap-3">
                <div>
                  <div className="text-xs uppercase tracking-wide text-muted-foreground">Application settings</div>
                  <div className="mt-1 text-base font-medium">Global configuration</div>
                </div>

                <div className="flex items-center gap-2 rounded-2xl bg-muted p-1 text-xs">
                  {globalSettingsTabs.map((tab) => (
                    <button
                      key={tab}
                      type="button"
                      onClick={() => onTabChange(tab)}
                      className={cn(
                        "rounded-xl px-3 py-2 transition",
                        globalSettingsTab === tab
                          ? "bg-card text-foreground shadow-sm"
                          : "text-muted-foreground hover:text-foreground",
                      )}
                    >
                      {tab === "providers" ? "Providers" : tab === "mcp" ? "Mcp" : "Skills"}
                    </button>
                  ))}
                </div>
              </div>

              <div className="grid gap-2 text-xs text-muted-foreground">
                <div>Config file: {configuration?.filePath ?? "Loading..."}</div>
                <div>Changes here apply to all conversations unless a conversation overrides its own provider or model.</div>
              </div>

              {globalSettingsMessage ? (
                <div className="rounded-2xl border border-emerald-500/30 bg-emerald-500/10 p-3 text-emerald-700 dark:text-emerald-300">
                  {globalSettingsMessage}
                </div>
              ) : null}

              {globalSettingsError ? (
                <div className="rounded-2xl border border-red-500/30 bg-red-500/10 p-3 text-red-700 dark:text-red-300">
                  {globalSettingsError}
                </div>
              ) : null}

              {configurationError && !globalSettingsError ? (
                <div className="rounded-2xl border border-red-500/30 bg-red-500/10 p-3 text-red-700 dark:text-red-300">
                  {configurationError}
                </div>
              ) : null}

              {isLoadingConfiguration ? (
                <div className="text-sm text-muted-foreground">Loading configuration...</div>
              ) : null}

              {globalSettingsTab === "providers" ? (
                <section className="rounded-2xl border border-border bg-background/50 p-4">
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <div className="text-xs uppercase tracking-wide text-muted-foreground">Providers</div>
                      <div className="mt-1 text-base font-medium">Provider configuration</div>
                    </div>
                    <div className="flex shrink-0 flex-wrap items-start gap-2">
                      <button
                        type="button"
                        onClick={() => void onRefreshConfiguration()}
                        className={secondaryButtonClassName}
                      >
                        Refresh
                      </button>
                      <button
                        type="button"
                        onClick={onAddProvider}
                        className={secondaryButtonClassName}
                      >
                        Add provider
                      </button>
                      <button
                        type="button"
                        onClick={() => void onSaveConfiguration()}
                        disabled={!configurationDraft || isSavingConfiguration}
                        className={primaryButtonClassName}
                      >
                        {isSavingConfiguration ? "Saving..." : "Save config"}
                      </button>
                    </div>
                  </div>

                  {configurationDraft ? (
                    <div className="mt-4 grid gap-4">
                      <label className="grid gap-2">
                        <FieldLabel label="Current default provider" />
                        <select
                          value={configurationDraft.currentDefaultProvider ?? ""}
                          onChange={(event) =>
                            onConfigurationDraftChange({
                              ...configurationDraft,
                              currentDefaultProvider: event.target.value || undefined,
                            })
                          }
                          className={singleLineFieldClassName}
                        >
                          <option value="">None</option>
                          {configurationDraft.providers.map((provider) => (
                            <option key={provider.name || `default-${provider.provider.type}`} value={provider.name}>
                              {provider.name || "Unnamed provider"}
                            </option>
                          ))}
                        </select>
                      </label>

                      {configurationDraft.providers.length === 0 ? (
                        <div className="rounded-2xl border border-dashed border-border p-4 text-sm text-muted-foreground">
                          <div className="font-medium text-foreground">Configure a provider to get started.</div>
                          <div className="mt-1">Add your first provider, set a default model, save the configuration, and then create your first session.</div>
                        </div>
                      ) : null}

                      {configurationDraft.providers.map((provider, index) => {
                        const providerFamily = getProviderFamily(provider.provider.type);
                        const providerApiType = getProviderApiType(provider.provider.type) ?? "chat";
                        const endpointFieldMeta = getProviderFieldMeta(provider.provider.type, "endpoint");
                        const apiKeyFieldMeta = getProviderFieldMeta(provider.provider.type, "apiKey");
                        const deploymentFieldMeta = getProviderFieldMeta(provider.provider.type, "deployment");
                        const providerModels = getProviderModels(provider);

                        return (
                          <div key={`provider-${index}`} className="rounded-2xl border border-border bg-card p-4">
                          <div className="flex items-center justify-between gap-3">
                            <div className="text-sm font-medium">{provider.name || `Provider ${index + 1}`}</div>
                            <div className="flex gap-2">
                              <button
                                type="button"
                                onClick={() => onRemoveProvider(index)}
                                className={compactButtonClassName}
                              >
                                Remove
                              </button>
                            </div>
                          </div>

                          <div className="mt-4 grid gap-3 md:grid-cols-2">
                            <label className="grid gap-2">
                              <FieldLabel label="Name" />
                              <input
                                value={provider.name}
                                onChange={(event) => onConfigurationFieldChange(index, "name", event.target.value)}
                                className={singleLineFieldClassName}
                              />
                            </label>

                            <label className="grid gap-2">
                              <FieldLabel label="Provider" />
                              <select
                                value={providerFamily ?? ""}
                                onChange={(event) =>
                                  onProviderConfigChange(
                                    index,
                                    "type",
                                    toProviderType((event.target.value || undefined) as ProviderFamily | undefined, "chat") ?? "",
                                  )}
                                className={singleLineFieldClassName}
                              >
                                <option value="">Select provider</option>
                                {providerFamilies.map((providerType) => (
                                  <option key={providerType} value={providerType}>
                                    {getProviderFamilyLabel(providerType)}
                                  </option>
                                ))}
                              </select>
                            </label>

                            {supportsProviderApiType(providerFamily) ? (
                              <label className="grid gap-2">
                                <FieldLabel label="API type" />
                                <select
                                  value={providerApiType}
                                  onChange={(event) =>
                                    onProviderConfigChange(
                                      index,
                                      "type",
                                      toProviderType(providerFamily, event.target.value as ProviderApiType) ?? "",
                                    )}
                                   className={singleLineFieldClassName}
                                >
                                  {providerApiTypes.map((apiType) => (
                                    <option key={apiType} value={apiType}>
                                      {getProviderApiTypeLabel(apiType)}
                                    </option>
                                  ))}
                                </select>
                              </label>
                            ) : null}

                            {isProviderFieldVisible(provider.provider.type, "endpoint") ? (
                              <label className="grid gap-2">
                                <FieldLabel label={endpointFieldMeta.label} status={endpointFieldMeta.status} />
                                <input
                                  value={provider.provider.endpoint ?? ""}
                                  onChange={(event) => onProviderConfigChange(index, "endpoint", event.target.value)}
                                  placeholder={endpointFieldMeta.placeholder}
                                  className={singleLineFieldClassName}
                                />
                              </label>
                            ) : null}

                            {isProviderFieldVisible(provider.provider.type, "apiKey") ? (
                              <label className="grid gap-2">
                                <FieldLabel label={apiKeyFieldMeta.label} status={apiKeyFieldMeta.status} />
                                <input
                                  value={provider.provider.apiKey ?? ""}
                                  onChange={(event) => onProviderConfigChange(index, "apiKey", event.target.value)}
                                  placeholder={apiKeyFieldMeta.placeholder ?? "Set API key"}
                                  className={singleLineFieldClassName}
                                />
                              </label>
                            ) : null}

                            {isProviderFieldVisible(provider.provider.type, "deployment") ? (
                              <label className="grid gap-2">
                                <FieldLabel label={deploymentFieldMeta.label} status={deploymentFieldMeta.status} />
                                <input
                                  value={provider.provider.deployment ?? ""}
                                  onChange={(event) => onProviderConfigChange(index, "deployment", event.target.value)}
                                  placeholder={deploymentFieldMeta.placeholder}
                                  className={singleLineFieldClassName}
                                />
                              </label>
                            ) : null}
                          </div>

                          {isProviderFieldVisible(provider.provider.type, "model") ? (
                            <div className="mt-3 rounded-2xl border border-border bg-background p-3">
                              <div className="flex flex-wrap items-start justify-between gap-3">
                                <div>
                                  <FieldLabel label="Available models" />
                                  <div className="mt-1 text-xs text-muted-foreground">
                                    Configure one or more model IDs and choose the default used by new sessions.
                                  </div>
                                </div>
                              </div>

                              <div className="mt-3 grid gap-2">
                                {providerModels.length ? providerModels.map((model, modelIndex) => (
                                  <div key={`${model}-${modelIndex}`} className="rounded-xl border border-border/70 bg-card px-3 py-2">
                                    <div className="grid gap-2 sm:grid-cols-[1fr_auto_auto_auto] sm:items-center">
                                      <input
                                        value={model}
                                        onChange={(event) => {
                                          const nextModels = providerModels.map((item, itemIndex) => itemIndex === modelIndex ? event.target.value : item);
                                          const nextDefaultModel = (provider.provider.defaultModel ?? providerModels[0]) === model
                                            ? event.target.value.trim()
                                            : provider.provider.defaultModel;
                                          updateProviderModels(index, provider, nextModels, nextDefaultModel);
                                        }}
                                        className="h-9 min-w-0 rounded-lg border border-border bg-background px-3 text-sm outline-none focus:ring-2 focus:ring-primary"
                                      />
                                      <label className="inline-flex h-9 items-center gap-2 rounded-lg border border-border px-3 text-xs text-muted-foreground">
                                        <input
                                          type="radio"
                                          name={`default-model-${index}`}
                                          checked={(provider.provider.defaultModel ?? providerModels[0]) === model}
                                          onChange={() => updateProviderModels(index, provider, providerModels, model)}
                                        />
                                        Default
                                      </label>
                                      <button
                                        type="button"
                                        onClick={() => void onTestProvider(index, model)}
                                        disabled={testingProviderIndexes[getProviderTestKey(index, model)]}
                                        className={compactButtonClassName}
                                      >
                                        {testingProviderIndexes[getProviderTestKey(index, model)] ? "Testing..." : "Test"}
                                      </button>
                                      <button
                                        type="button"
                                        onClick={() => removeProviderModel(index, provider, model)}
                                        className={compactButtonClassName}
                                      >
                                        Remove
                                      </button>
                                    </div>
                                    {providerTestResults[getProviderTestKey(index, model)] ? (
                                      <div className="mt-2 rounded-lg border border-border bg-background px-3 py-2 text-xs text-muted-foreground">
                                        {providerTestResults[getProviderTestKey(index, model)]}
                                      </div>
                                    ) : null}
                                  </div>
                                )) : (
                                  <div className="rounded-xl border border-dashed border-border p-3 text-sm text-muted-foreground">
                                    No models configured yet. Add a model to enable this provider.
                                  </div>
                                )}
                              </div>

                              <div className="mt-3 flex flex-col gap-2 sm:flex-row">
                                <input
                                  value={newProviderModels[index] ?? ""}
                                  onChange={(event) => setNewProviderModels((previous) => ({ ...previous, [index]: event.target.value }))}
                                  onKeyDown={(event) => {
                                    if (event.key === "Enter") {
                                      event.preventDefault();
                                      addProviderModel(index, provider);
                                    }
                                  }}
                                  placeholder="Add model ID"
                                  className={singleLineFieldClassName}
                                />
                                <button
                                  type="button"
                                  onClick={() => addProviderModel(index, provider)}
                                  className={secondaryButtonClassName}
                                >
                                  Add model
                                </button>
                              </div>
                            </div>
                          ) : null}

                          {isProviderFieldVisible(provider.provider.type, "useDefaultAzureCredential") ? (
                            <div className="mt-3 flex items-center justify-between gap-3 rounded-xl border border-border bg-background px-3 py-2 text-sm">
                              <div>
                                <div className="font-medium">Use default Azure credential</div>
                                <div className="text-xs text-muted-foreground">Use Microsoft Entra authentication instead of an API key.</div>
                              </div>
                              <ToggleSwitch
                                checked={provider.provider.useDefaultAzureCredential}
                                onCheckedChange={(checked) => onProviderConfigChange(index, "useDefaultAzureCredential", checked)}
                              />
                            </div>
                          ) : null}

                          {providerTestResults[getProviderTestKey(index)] ? (
                            <div className="mt-3 rounded-xl border border-border bg-background px-3 py-2 text-xs text-muted-foreground">
                              {providerTestResults[getProviderTestKey(index)]}
                            </div>
                          ) : null}
                          </div>
                        );
                      })}
                    </div>
                  ) : null}
                </section>
              ) : null}

              {globalSettingsTab === "mcp" ? (
                <section className="rounded-2xl border border-border bg-background/50 p-4">
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <div className="text-xs uppercase tracking-wide text-muted-foreground">MCP</div>
                      <div className="mt-1 text-base font-medium">HTTP MCP server configuration</div>
                    </div>
                    <div className="flex shrink-0 flex-wrap items-start gap-2">
                      <button
                        type="button"
                        onClick={onAddMcpServer}
                        className={secondaryButtonClassName}
                      >
                        Add server
                      </button>
                      <button
                        type="button"
                        onClick={() => void onSaveConfiguration()}
                        disabled={!configurationDraft || isSavingConfiguration}
                        className={primaryButtonClassName}
                      >
                        {isSavingConfiguration ? "Saving..." : "Save config"}
                      </button>
                    </div>
                  </div>

                  {configurationDraft?.mcpServers.length ? (
                    <div className="mt-4 grid gap-4">
                      {configurationDraft.mcpServers.map((server, index) => (
                        <div
                          key={`${server.name}-${index}`}
                          className={cn(
                            "rounded-2xl border border-border p-4 transition-opacity",
                            server.enabled ? "bg-card" : "bg-card/60 opacity-80",
                          )}
                        >
                          <div className="flex items-start justify-between gap-3">
                            <div>
                              <div className="text-sm font-medium">{server.name || `MCP Server ${index + 1}`}</div>
                              <div className="mt-1 text-xs text-muted-foreground">
                                {server.enabled ? "On · Connected automatically after saving." : "Off · Not connected until turned on."}
                              </div>
                            </div>
                            <div className="flex shrink-0 flex-wrap items-start gap-2">
                              <ToggleSwitch
                                checked={server.enabled}
                                onCheckedChange={(checked) => onMcpServerChange(index, "enabled", checked)}
                              />
                              <button
                                type="button"
                                onClick={() => void onTestMcpServer(index)}
                                className={compactButtonClassName}
                              >
                                Test
                              </button>
                              <button
                                type="button"
                                onClick={() => onRemoveMcpServer(index)}
                                className={compactButtonClassName}
                              >
                                Remove
                              </button>
                            </div>
                          </div>

                          <div className="mt-4 grid gap-3 md:grid-cols-2">
                            <label className="grid gap-2">
                              <FieldLabel label="Name" />
                              <input
                                value={server.name ?? ""}
                                onChange={(event) => onMcpServerChange(index, "name", event.target.value)}
                                className={singleLineFieldClassName}
                              />
                            </label>

                            <label className="grid gap-2 md:col-span-2">
                              <FieldLabel label="HTTP endpoint URL" />
                              <input
                                value={server.url}
                                onChange={(event) => onMcpServerChange(index, "url", event.target.value)}
                                placeholder="https://example.com/mcp"
                                className={singleLineFieldClassName}
                              />
                              <span className="text-xs text-muted-foreground">
                                Use the MCP server&apos;s HTTP endpoint. Configured MCP servers are connected automatically. Streamable HTTP is preferred and SSE fallback is handled by the backend transport.
                              </span>
                            </label>
                          </div>

                          <label className="mt-3 grid gap-2">
                            <FieldLabel label="HTTP headers" />
                            <textarea
                              value={Object.entries(server.headers)
                                .map(([key, value]) => `${key}=${value}`)
                                .join("\n")}
                              onChange={(event) => onMcpServerChange(index, "headers", event.target.value)}
                              placeholder="Authorization=Bearer ..."
                              className="min-h-24 rounded-xl border border-border bg-background px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary"
                            />
                            <span className="text-xs text-muted-foreground">
                              Optional. Enter one header per line in the format <code>Header-Name=value</code>.
                            </span>
                          </label>

                          {mcpTestResults[index] ? (
                            <div className="mt-3 rounded-2xl border border-border bg-background/60 p-3 text-xs text-muted-foreground">
                              {mcpTestResults[index]}
                            </div>
                          ) : null}
                        </div>
                      ))}
                    </div>
                  ) : (
                    <div className="mt-4 rounded-2xl border border-dashed border-border p-4 text-sm text-muted-foreground">
                      No MCP servers configured yet.
                    </div>
                  )}
                </section>
              ) : null}

              {globalSettingsTab === "skills" ? (
                <section className="rounded-2xl border border-border bg-background/50 p-4">
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <div className="text-xs uppercase tracking-wide text-muted-foreground">Skills</div>
                      <div className="mt-1 text-base font-medium">Imported skills</div>
                    </div>
                    <button
                      type="button"
                      onClick={() => void onSaveConfiguration()}
                      disabled={!configurationDraft || isSavingConfiguration}
                      className={primaryButtonClassName}
                    >
                      {isSavingConfiguration ? "Saving..." : "Save config"}
                    </button>
                  </div>

                  <div className="mt-4 grid gap-3">
                    <label className="grid gap-2">
                      <FieldLabel label="Skill import path" />
                      <div className="flex gap-2">
                        <input
                          value={skillImportPath}
                          onChange={(event) => onSkillImportPathChange(event.target.value)}
                          placeholder="/path/to/skill-folder"
                          className={cn("min-w-0 flex-1", singleLineFieldClassName)}
                        />
                        <button
                          type="button"
                          onClick={() => void onOpenSkillDirectoryPicker()}
                          className={secondaryButtonClassName}
                        >
                          Browse
                        </button>
                        <button
                          type="button"
                          onClick={() => void onImportSkill()}
                          disabled={!skillImportPath.trim() || isImportingSkill}
                          className={cn(secondaryButtonClassName, "disabled:cursor-not-allowed disabled:opacity-50")}
                        >
                          {isImportingSkill ? "Importing..." : "Import"}
                        </button>
                      </div>
                    </label>

                    <div className="grid gap-1 text-xs text-muted-foreground">
                      <div>Storage path: {configurationDraft?.skills.storagePath ?? "Default location"}</div>
                      <div>Import validates the skill folder and updates the shared skill catalog.</div>
                    </div>
                  </div>

                    {configurationDraft?.skills.imported.length ? (
                      <div className="mt-4 grid gap-3">
                        {configurationDraft.skills.imported.map((skill, index) => (
                          <div
                            key={skill.path}
                            className={cn(
                              "rounded-2xl border border-border p-4 transition-opacity",
                              skill.enabled ? "bg-card" : "bg-card/60 opacity-80",
                            )}
                          >
                            <div className="flex items-start justify-between gap-3">
                              <div className="min-w-0">
                                <div className="text-sm font-medium">{skill.name}</div>
                                <div className="mt-1 text-xs text-muted-foreground">{skill.path}</div>
                                <div className="mt-2 text-xs text-muted-foreground">
                                  {skill.enabled ? "On · Loaded for all conversations." : "Off · Kept in the catalog but not loaded."}
                                </div>
                                {skill.description ? <div className="mt-2 text-sm text-muted-foreground">{skill.description}</div> : null}
                              </div>
                              <div className="flex shrink-0 items-start gap-2">
                                <ToggleSwitch
                                  checked={skill.enabled}
                                  onCheckedChange={(checked) => onGlobalSkillChange(index, "enabled", checked)}
                                />
                                <button
                                  type="button"
                                  onClick={() => void onRemoveGlobalSkill(skill.name)}
                                  className={compactButtonClassName}
                                >
                                  Remove
                                </button>
                              </div>
                            </div>
                          </div>
                        ))}
                      </div>
                  ) : (
                    <div className="mt-4 rounded-2xl border border-dashed border-border p-4 text-sm text-muted-foreground">
                      No imported skills yet.
                    </div>
                  )}
                </section>
              ) : null}
            </div>
          </div>
        </div>
      </div>
  );
}
