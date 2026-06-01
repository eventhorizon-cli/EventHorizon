import { X } from "lucide-react";
import { ToggleSwitch } from "@/components/settings/ToggleSwitch";
import { cn } from "@/utils/cn";
import {
  getProviderFieldMeta,
  globalSettingsTabs,
  isProviderFieldVisible,
  providerTypes,
} from "@/utils/configuration";
import type {
  AppConfiguration,
  ImportedSkill,
  McpServerConfig,
  ProviderEntry,
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
  providerTestResults: Record<number, string>;
  testingProviderIndexes: Record<number, boolean>;
  onClose: () => void;
  onTabChange: (tab: GlobalSettingsTab) => void;
  onRefreshConfiguration: () => Promise<void> | void;
  onSaveConfiguration: () => Promise<void> | void;
  onConfigurationDraftChange: (configuration: AppConfiguration) => void;
  onAddProvider: () => void;
  onRemoveProvider: (index: number) => void;
  onConfigurationFieldChange: (index: number, field: keyof ProviderEntry, value: string) => void;
  onProviderConfigChange: (index: number, field: keyof ProviderEntry["provider"], value: string | boolean) => void;
  onTestProvider: (index: number) => Promise<void> | void;
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
  if (!open) {
    return null;
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
                  <div className="flex items-center justify-between gap-3">
                    <div>
                      <div className="text-xs uppercase tracking-wide text-muted-foreground">Providers</div>
                      <div className="mt-1 text-base font-medium">Provider configuration</div>
                    </div>
                    <div className="flex gap-2">
                      <button
                        type="button"
                        onClick={() => void onRefreshConfiguration()}
                        className="rounded-xl border border-border px-3 py-2 text-xs font-medium transition hover:bg-muted"
                      >
                        Refresh
                      </button>
                      <button
                        type="button"
                        onClick={onAddProvider}
                        className="rounded-xl border border-border px-3 py-2 text-xs font-medium transition hover:bg-muted"
                      >
                        Add provider
                      </button>
                      <button
                        type="button"
                        onClick={() => void onSaveConfiguration()}
                        disabled={!configurationDraft || isSavingConfiguration}
                        className="rounded-xl bg-primary px-3 py-2 text-xs font-medium text-primary-foreground transition hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-50"
                      >
                        {isSavingConfiguration ? "Saving..." : "Save config"}
                      </button>
                    </div>
                  </div>

                  {configurationDraft ? (
                    <div className="mt-4 grid gap-4">
                      <label className="grid gap-2">
                        <span className="text-xs uppercase tracking-wide text-muted-foreground">Current default provider</span>
                        <select
                          value={configurationDraft.currentDefaultProvider ?? ""}
                          onChange={(event) =>
                            onConfigurationDraftChange({
                              ...configurationDraft,
                              currentDefaultProvider: event.target.value || undefined,
                            })
                          }
                          className="rounded-xl border border-border bg-background px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary"
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
                          No providers configured yet.
                        </div>
                      ) : null}

                      {configurationDraft.providers.map((provider, index) => (
                        <div key={`provider-${index}`} className="rounded-2xl border border-border bg-card p-4">
                          <div className="flex items-center justify-between gap-3">
                            <div className="text-sm font-medium">{provider.name || `Provider ${index + 1}`}</div>
                            <div className="flex gap-2">
                              <button
                                type="button"
                                onClick={() => void onTestProvider(index)}
                                disabled={testingProviderIndexes[index]}
                                className="rounded-xl border border-border px-2.5 py-1.5 text-xs text-muted-foreground transition hover:bg-muted disabled:cursor-not-allowed disabled:opacity-50"
                              >
                                {testingProviderIndexes[index] ? "Testing..." : "Test"}
                              </button>
                              <button
                                type="button"
                                onClick={() => onRemoveProvider(index)}
                                className="rounded-xl border border-border px-2.5 py-1.5 text-xs text-muted-foreground transition hover:bg-muted"
                              >
                                Remove
                              </button>
                            </div>
                          </div>

                          <div className="mt-4 grid gap-3 md:grid-cols-2">
                            <label className="grid gap-2">
                              <span className="text-xs uppercase tracking-wide text-muted-foreground">Name</span>
                              <input
                                value={provider.name}
                                onChange={(event) => onConfigurationFieldChange(index, "name", event.target.value)}
                                className="rounded-xl border border-border bg-background px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary"
                              />
                            </label>

                            <label className="grid gap-2">
                              <span className="text-xs uppercase tracking-wide text-muted-foreground">Type</span>
                              <select
                                value={provider.provider.type ?? ""}
                                onChange={(event) => onProviderConfigChange(index, "type", event.target.value)}
                                className="rounded-xl border border-border bg-background px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary"
                              >
                                <option value="">Select provider type</option>
                                {providerTypes.map((providerType) => (
                                  <option key={providerType} value={providerType}>
                                    {providerType}
                                  </option>
                                ))}
                              </select>
                            </label>

                            {isProviderFieldVisible(provider.provider.type, "model") ? (
                              <label className="grid gap-2">
                                <span className="text-xs uppercase tracking-wide text-muted-foreground">
                                  {getProviderFieldMeta(provider.provider.type, "model").label}
                                </span>
                                <input
                                  value={provider.provider.model ?? ""}
                                  onChange={(event) => onProviderConfigChange(index, "model", event.target.value)}
                                  className="rounded-xl border border-border bg-background px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary"
                                />
                                <span className="text-xs text-muted-foreground">
                                  {getProviderFieldMeta(provider.provider.type, "model").hint}
                                </span>
                              </label>
                            ) : null}

                            {isProviderFieldVisible(provider.provider.type, "endpoint") ? (
                              <label className="grid gap-2">
                                <span className="text-xs uppercase tracking-wide text-muted-foreground">
                                  {getProviderFieldMeta(provider.provider.type, "endpoint").label}
                                </span>
                                <input
                                  value={provider.provider.endpoint ?? ""}
                                  onChange={(event) => onProviderConfigChange(index, "endpoint", event.target.value)}
                                  className="rounded-xl border border-border bg-background px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary"
                                />
                                <span className="text-xs text-muted-foreground">
                                  {getProviderFieldMeta(provider.provider.type, "endpoint").hint}
                                </span>
                              </label>
                            ) : null}

                            {isProviderFieldVisible(provider.provider.type, "apiKey") ? (
                              <label className="grid gap-2">
                                <span className="text-xs uppercase tracking-wide text-muted-foreground">
                                  {getProviderFieldMeta(provider.provider.type, "apiKey").label}
                                </span>
                                <input
                                  value={provider.provider.apiKey ?? ""}
                                  onChange={(event) => onProviderConfigChange(index, "apiKey", event.target.value)}
                                  placeholder="Set API key"
                                  className="rounded-xl border border-border bg-background px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary"
                                />
                                <span className="text-xs text-muted-foreground">
                                  {getProviderFieldMeta(provider.provider.type, "apiKey").hint}
                                </span>
                              </label>
                            ) : null}

                            {isProviderFieldVisible(provider.provider.type, "deployment") ? (
                              <label className="grid gap-2">
                                <span className="text-xs uppercase tracking-wide text-muted-foreground">
                                  {getProviderFieldMeta(provider.provider.type, "deployment").label}
                                </span>
                                <input
                                  value={provider.provider.deployment ?? ""}
                                  onChange={(event) => onProviderConfigChange(index, "deployment", event.target.value)}
                                  className="rounded-xl border border-border bg-background px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary"
                                />
                                <span className="text-xs text-muted-foreground">
                                  {getProviderFieldMeta(provider.provider.type, "deployment").hint}
                                </span>
                              </label>
                            ) : null}
                          </div>

                          <label className="mt-3 grid gap-2">
                            <span className="text-xs uppercase tracking-wide text-muted-foreground">Available models</span>
                            <textarea
                              value={provider.provider.models.join("\n")}
                              onChange={(event) => onProviderConfigChange(index, "models", event.target.value)}
                              placeholder="One model ID per line"
                              className="min-h-28 rounded-xl border border-border bg-background px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary"
                            />
                          </label>

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

                          {providerTestResults[index] ? (
                            <div className="mt-3 rounded-xl border border-border bg-background px-3 py-2 text-xs text-muted-foreground">
                              {providerTestResults[index]}
                            </div>
                          ) : null}
                        </div>
                      ))}
                    </div>
                  ) : null}
                </section>
              ) : null}

              {globalSettingsTab === "mcp" ? (
                <section className="rounded-2xl border border-border bg-background/50 p-4">
                  <div className="flex items-center justify-between gap-3">
                    <div>
                      <div className="text-xs uppercase tracking-wide text-muted-foreground">MCP</div>
                      <div className="mt-1 text-base font-medium">HTTP MCP server configuration</div>
                    </div>
                    <div className="flex gap-2">
                      <button
                        type="button"
                        onClick={onAddMcpServer}
                        className="rounded-xl border border-border px-3 py-2 text-xs font-medium transition hover:bg-muted"
                      >
                        Add server
                      </button>
                      <button
                        type="button"
                        onClick={() => void onSaveConfiguration()}
                        disabled={!configurationDraft || isSavingConfiguration}
                        className="rounded-xl bg-primary px-3 py-2 text-xs font-medium text-primary-foreground transition hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-50"
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
                          <div className="flex items-center justify-between gap-3">
                            <div>
                              <div className="text-sm font-medium">{server.name || `MCP Server ${index + 1}`}</div>
                              <div className="mt-1 text-xs text-muted-foreground">
                                {server.enabled ? "On · Connected automatically after saving." : "Off · Not connected until turned on."}
                              </div>
                            </div>
                            <div className="flex gap-2">
                              <ToggleSwitch
                                checked={server.enabled}
                                onCheckedChange={(checked) => onMcpServerChange(index, "enabled", checked)}
                              />
                              <button
                                type="button"
                                onClick={() => void onTestMcpServer(index)}
                                className="rounded-xl border border-border px-2.5 py-1.5 text-xs text-muted-foreground transition hover:bg-muted"
                              >
                                Test
                              </button>
                              <button
                                type="button"
                                onClick={() => onRemoveMcpServer(index)}
                                className="rounded-xl border border-border px-2.5 py-1.5 text-xs text-muted-foreground transition hover:bg-muted"
                              >
                                Remove
                              </button>
                            </div>
                          </div>

                          <div className="mt-4 grid gap-3 md:grid-cols-2">
                            <label className="grid gap-2">
                              <span className="text-xs uppercase tracking-wide text-muted-foreground">Name</span>
                              <input
                                value={server.name ?? ""}
                                onChange={(event) => onMcpServerChange(index, "name", event.target.value)}
                                className="rounded-xl border border-border bg-background px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary"
                              />
                            </label>

                            <label className="grid gap-2 md:col-span-2">
                              <span className="text-xs uppercase tracking-wide text-muted-foreground">HTTP endpoint URL</span>
                              <input
                                value={server.url}
                                onChange={(event) => onMcpServerChange(index, "url", event.target.value)}
                                placeholder="https://example.com/mcp"
                                className="rounded-xl border border-border bg-background px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary"
                              />
                              <span className="text-xs text-muted-foreground">
                                Use the MCP server&apos;s HTTP endpoint. Configured MCP servers are connected automatically. Streamable HTTP is preferred and SSE fallback is handled by the backend transport.
                              </span>
                            </label>
                          </div>

                          <label className="mt-3 grid gap-2">
                            <span className="text-xs uppercase tracking-wide text-muted-foreground">HTTP headers</span>
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
                  <div className="flex items-center justify-between gap-3">
                    <div>
                      <div className="text-xs uppercase tracking-wide text-muted-foreground">Skills</div>
                      <div className="mt-1 text-base font-medium">Imported skills</div>
                    </div>
                    <button
                      type="button"
                      onClick={() => void onSaveConfiguration()}
                      disabled={!configurationDraft || isSavingConfiguration}
                      className="rounded-xl bg-primary px-3 py-2 text-xs font-medium text-primary-foreground transition hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-50"
                    >
                      {isSavingConfiguration ? "Saving..." : "Save config"}
                    </button>
                  </div>

                  <div className="mt-4 grid gap-3">
                    <label className="grid gap-2">
                      <span className="text-xs uppercase tracking-wide text-muted-foreground">Skill import path</span>
                      <div className="flex gap-2">
                        <input
                          value={skillImportPath}
                          onChange={(event) => onSkillImportPathChange(event.target.value)}
                          placeholder="/path/to/skill-folder"
                          className="min-w-0 flex-1 rounded-xl border border-border bg-background px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary"
                        />
                        <button
                          type="button"
                          onClick={() => void onOpenSkillDirectoryPicker()}
                          className="rounded-xl border border-border px-3 py-2 text-xs font-medium transition hover:bg-muted"
                        >
                          Browse
                        </button>
                        <button
                          type="button"
                          onClick={() => void onImportSkill()}
                          disabled={!skillImportPath.trim() || isImportingSkill}
                          className="rounded-xl border border-border px-3 py-2 text-xs font-medium transition hover:bg-muted disabled:cursor-not-allowed disabled:opacity-50"
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
                              <div className="flex shrink-0 items-center gap-2">
                                <ToggleSwitch
                                  checked={skill.enabled}
                                  onCheckedChange={(checked) => onGlobalSkillChange(index, "enabled", checked)}
                                />
                                <button
                                  type="button"
                                  onClick={() => void onRemoveGlobalSkill(skill.name)}
                                  className="rounded-xl border border-border px-2.5 py-1.5 text-xs text-muted-foreground transition hover:bg-muted"
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
