import { cn } from "@/utils/cn";
import type {
  AgentPhase,
  AgentRun,
  AgentSessionDetail,
  AppConfiguration,
  ContextView,
  FileChange,
  ProviderEntry,
} from "@/types";

type ContextPanelProps = {
  contextView: ContextView;
  currentRun?: AgentRun;
  currentSession?: AgentSessionDetail;
  configuration?: AppConfiguration;
  configurationDraft?: AppConfiguration;
  detailsMessage?: string;
  detailsError?: string;
  sessionSettingsMessage?: string;
  sessionSettingsError?: string;
  isUpdatingSession: boolean;
  isImportingSkill: boolean;
  sessionTitleInput: string;
  skillImportPath: string;
  selectedProviderName?: string;
  selectedProviderType?: string;
  availableModels: string[];
  sessionModelWarning?: string;
  selectedProviderDefaultModel?: string;
  changes: FileChange[];
  selectedFile?: string;
  phase: AgentPhase;
  onContextViewChange: (view: ContextView) => void;
  onSessionTitleInputChange: (value: string) => void;
  onSkillImportPathChange: (value: string) => void;
  onSaveSessionTitle: () => Promise<void> | void;
  onDeleteSession: () => Promise<void> | void;
  onChangeSessionProvider: (providerName: string) => Promise<void> | void;
  onChangeSessionModel: (model: string) => Promise<void> | void;
  onOpenSkillDirectoryPicker: () => Promise<void> | void;
  onImportSkill: () => Promise<void> | void;
  onRemoveSessionSkill: (skillName: string) => Promise<void> | void;
  onOpenDiff: (change: FileChange) => Promise<void> | void;
};

export function ContextPanel({
  contextView,
  currentRun,
  currentSession,
  configuration,
  configurationDraft,
  detailsMessage,
  detailsError,
  sessionSettingsMessage,
  sessionSettingsError,
  isUpdatingSession,
  isImportingSkill,
  sessionTitleInput,
  skillImportPath,
  selectedProviderName,
  selectedProviderType,
  availableModels,
  sessionModelWarning,
  selectedProviderDefaultModel,
  changes,
  selectedFile,
  phase,
  onContextViewChange,
  onSessionTitleInputChange,
  onSkillImportPathChange,
  onSaveSessionTitle,
  onDeleteSession,
  onChangeSessionProvider,
  onChangeSessionModel,
  onOpenSkillDirectoryPicker,
  onImportSkill,
  onRemoveSessionSkill,
  onOpenDiff,
}: ContextPanelProps) {
  return (
    <aside className="flex h-full min-h-0 min-w-0 flex-col overflow-hidden rounded-3xl border border-border/70 bg-card/95 shadow-sm">
      <div className="flex shrink-0 items-center border-b border-border/70 px-4 py-3">
        <div className="flex gap-1 rounded-full bg-muted p-1 text-xs">
          {(["overview", "files", "settings"] as const).map((view) => (
            <button
              key={view}
              type="button"
              onClick={() => onContextViewChange(view)}
              className={cn(
                "rounded-full px-3 py-1.5 capitalize transition",
                contextView === view ? "bg-card text-foreground shadow-sm" : "text-muted-foreground hover:text-foreground",
              )}
            >
              {view}
            </button>
          ))}
        </div>
      </div>

      <div className="min-h-0 flex-1 overflow-y-auto p-4">
        {contextView === "overview" ? (
          <div className="space-y-4 text-sm">
            <section className="rounded-2xl border border-border bg-background/50 p-4">
              <div className="text-xs uppercase tracking-wide text-muted-foreground">Current task</div>
              <div className="mt-2 font-medium">{currentRun?.task ?? "No active run"}</div>
            </section>

            <section className="rounded-2xl border border-border bg-background/50 p-4">
              <div className="text-xs uppercase tracking-wide text-muted-foreground">Workspace directory</div>
              <div className="mt-2 break-all font-medium">{currentSession?.workspaceRoot ?? "Not selected"}</div>
            </section>

            <section className="rounded-2xl border border-border bg-background/50 p-4">
              <div className="text-xs uppercase tracking-wide text-muted-foreground">Status</div>
              <div className="mt-2 font-medium capitalize">{currentRun?.status ?? "idle"}</div>
              <div className="mt-1 text-muted-foreground">Phase: {phase}</div>
            </section>

            <section className="rounded-2xl border border-border bg-background/50 p-4">
              <div className="text-xs uppercase tracking-wide text-muted-foreground">Changes</div>
              <div className="mt-2 font-medium">{changes.length} files</div>
            </section>
          </div>
        ) : null}

        {contextView === "files" || contextView === "diff" ? (
          <div className="space-y-3">
            <div className="rounded-2xl border border-dashed border-border px-4 py-3 text-sm text-muted-foreground">
              Click a changed file to open its diff in a modal. You can switch between files from the modal list.
            </div>

            {changes.length === 0 ? (
              <div className="rounded-2xl border border-dashed border-border p-4 text-sm text-muted-foreground">No file changes yet.</div>
            ) : null}

            {changes.map((change) => (
              <button
                key={change.path}
                type="button"
                onClick={() => void onOpenDiff(change)}
                className={cn(
                  "flex w-full items-center justify-between gap-3 rounded-2xl border px-3 py-3 text-left transition hover:bg-muted",
                  selectedFile === change.path ? "border-primary bg-primary/10" : "border-border bg-background/50",
                )}
              >
                <div className="min-w-0">
                  <div className="truncate text-sm font-medium">{change.path}</div>
                  <div className="text-xs text-muted-foreground">{change.status}</div>
                </div>
                <div className="shrink-0 text-xs text-muted-foreground">+{change.additions ?? 0} / -{change.deletions ?? 0}</div>
              </button>
            ))}
          </div>
        ) : null}

        {contextView === "settings" ? (
          <div className="space-y-4 text-sm">
            {detailsMessage ? (
              <div className="rounded-2xl border border-emerald-500/30 bg-emerald-500/10 p-3 text-emerald-700 dark:text-emerald-300">
                {detailsMessage}
              </div>
            ) : null}

            {detailsError ? (
              <div className="rounded-2xl border border-red-500/30 bg-red-500/10 p-3 text-red-700 dark:text-red-300">{detailsError}</div>
            ) : null}

            {sessionSettingsMessage ? (
              <div className="rounded-2xl border border-emerald-500/30 bg-emerald-500/10 p-3 text-emerald-700 dark:text-emerald-300">
                {sessionSettingsMessage}
              </div>
            ) : null}

            {sessionSettingsError ? (
              <div className="rounded-2xl border border-red-500/30 bg-red-500/10 p-3 text-red-700 dark:text-red-300">{sessionSettingsError}</div>
            ) : null}

            <section className="rounded-2xl border border-border bg-background/50 p-4">
              <div className="flex items-center justify-between gap-3">
                <div>
                  <div className="text-xs uppercase tracking-wide text-muted-foreground">Conversation settings</div>
                  <div className="mt-1 text-base font-medium">{currentSession?.title ?? "No conversation selected"}</div>
                </div>
                {currentSession && !currentSession.id.startsWith("draft_") ? (
                  <button
                    type="button"
                    onClick={() => void onDeleteSession()}
                    className="rounded-xl border border-red-500/30 px-3 py-2 text-xs font-medium text-red-600 transition hover:bg-red-500/10 dark:text-red-300"
                  >
                    Delete
                  </button>
                ) : null}
              </div>

              <div className="mt-4 grid gap-3">
                <label className="grid gap-2">
                  <span className="text-xs uppercase tracking-wide text-muted-foreground">Title</span>
                  <div className="flex gap-2">
                    <input
                      value={sessionTitleInput}
                      onChange={(event) => onSessionTitleInputChange(event.target.value)}
                      disabled={!currentSession || currentSession.id.startsWith("draft_") || isUpdatingSession}
                      className="min-w-0 flex-1 rounded-xl border border-border bg-background px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary"
                    />
                    <button
                      type="button"
                      onClick={() => void onSaveSessionTitle()}
                      disabled={!currentSession || currentSession.id.startsWith("draft_") || isUpdatingSession || !sessionTitleInput.trim()}
                      className="rounded-xl border border-border px-3 py-2 text-xs font-medium transition hover:bg-muted disabled:cursor-not-allowed disabled:opacity-50"
                    >
                      Save
                    </button>
                  </div>
                </label>

                <label className="grid gap-2">
                  <span className="text-xs uppercase tracking-wide text-muted-foreground">Provider</span>
                  <select
                    value={currentSession?.providerName ?? ""}
                    onChange={(event) => void onChangeSessionProvider(event.target.value)}
                    disabled={!currentSession || currentSession.id.startsWith("draft_") || isUpdatingSession}
                    className="rounded-xl border border-border bg-background px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary"
                  >
                    <option value="">
                      {configuration?.currentDefaultProvider ? `Use default (${configuration.currentDefaultProvider})` : "Use default provider"}
                    </option>
                    {(configurationDraft?.providers ?? configuration?.providers ?? []).map((provider: ProviderEntry) => (
                      <option key={provider.name || `provider-${provider.provider.type}`} value={provider.name}>
                        {provider.name || "Unnamed provider"}
                      </option>
                    ))}
                  </select>
                </label>

                <label className="grid gap-2">
                  <span className="text-xs uppercase tracking-wide text-muted-foreground">Model</span>
                  <select
                    value={currentSession?.model ?? ""}
                    onChange={(event) => void onChangeSessionModel(event.target.value)}
                    disabled={!currentSession || currentSession.id.startsWith("draft_") || isUpdatingSession}
                    className="rounded-xl border border-border bg-background px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary"
                  >
                    {availableModels.length === 0 ? <option value="">No configured models</option> : null}
                    {availableModels.map((model) => (
                      <option key={model} value={model}>
                        {model}
                      </option>
                    ))}
                  </select>
                  {sessionModelWarning ? (
                    <div className="flex items-center justify-between gap-3 rounded-xl border border-amber-500/30 bg-amber-500/10 px-3 py-2 text-xs text-amber-700 dark:text-amber-300">
                      <span>{sessionModelWarning}</span>
                      <button
                        type="button"
                        onClick={() => selectedProviderDefaultModel ? void onChangeSessionModel(selectedProviderDefaultModel) : undefined}
                        disabled={!selectedProviderDefaultModel || isUpdatingSession}
                        className="shrink-0 rounded-lg border border-amber-500/30 px-2.5 py-1 font-medium transition hover:bg-amber-500/10 disabled:cursor-not-allowed disabled:opacity-50"
                      >
                        Use provider default
                      </button>
                    </div>
                  ) : null}
                </label>

                <div className="grid gap-1 text-xs text-muted-foreground">
                  <div>Workspace directory: {currentSession?.workspaceRoot ?? "Not selected"}</div>
                  <div>Resolved provider: {selectedProviderName ?? "None"}</div>
                  <div>Provider type: {currentSession?.providerType ?? selectedProviderType ?? "Unknown"}</div>
                </div>
              </div>
            </section>

            <section className="rounded-2xl border border-border bg-background/50 p-4">
              <div>
                <div className="text-xs uppercase tracking-wide text-muted-foreground">Skills</div>
                <div className="mt-1 text-base font-medium">Global and session skills</div>
              </div>

              <div className="mt-4 grid gap-3">
                <label className="grid gap-2">
                  <span className="text-xs uppercase tracking-wide text-muted-foreground">Import skill path</span>
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
                      disabled={!skillImportPath.trim() || isImportingSkill || !currentSession || currentSession.id.startsWith("draft_")}
                      className="rounded-xl border border-border px-3 py-2 text-xs font-medium transition hover:bg-muted disabled:cursor-not-allowed disabled:opacity-50"
                    >
                      {isImportingSkill ? "Importing..." : "Import"}
                    </button>
                  </div>
                </label>

                <div className="grid gap-2 md:grid-cols-2">
                  <div className="rounded-2xl border border-border bg-card p-4">
                    <div className="text-xs uppercase tracking-wide text-muted-foreground">Global skills</div>
                    <div className="mt-3 grid gap-3">
                      {(configuration?.skills.imported ?? []).length ? (
                        (configuration?.skills.imported ?? []).map((skill) => (
                          <div key={`global-${skill.path}`} className="rounded-xl border border-border bg-background/60 p-3">
                            <div className="text-sm font-medium">{skill.name}</div>
                            <div className="mt-1 text-xs text-muted-foreground">{skill.path}</div>
                            {skill.description ? <div className="mt-2 text-sm text-muted-foreground">{skill.description}</div> : null}
                          </div>
                        ))
                      ) : (
                        <div className="rounded-xl border border-dashed border-border p-3 text-sm text-muted-foreground">No global skills.</div>
                      )}
                    </div>
                  </div>

                  <div className="rounded-2xl border border-border bg-card p-4">
                    <div className="text-xs uppercase tracking-wide text-muted-foreground">Session skills</div>
                    <div className="mt-3 grid gap-3">
                      {(currentSession?.sessionSkills.imported ?? []).length ? (
                        (currentSession?.sessionSkills.imported ?? []).map((skill) => (
                          <div key={`session-${skill.path}`} className="rounded-xl border border-border bg-background/60 p-3">
                            <div className="flex items-start justify-between gap-3">
                              <div className="min-w-0">
                                <div className="text-sm font-medium">{skill.name}</div>
                                <div className="mt-1 text-xs text-muted-foreground">{skill.path}</div>
                                {skill.description ? <div className="mt-2 text-sm text-muted-foreground">{skill.description}</div> : null}
                              </div>
                              <button
                                type="button"
                                onClick={() => void onRemoveSessionSkill(skill.name)}
                                className="rounded-xl border border-border px-2.5 py-1.5 text-xs text-muted-foreground transition hover:bg-muted"
                              >
                                Remove
                              </button>
                            </div>
                          </div>
                        ))
                      ) : (
                        <div className="rounded-xl border border-dashed border-border p-3 text-sm text-muted-foreground">No session skills.</div>
                      )}
                    </div>
                  </div>
                </div>
              </div>
            </section>
          </div>
        ) : null}
      </div>
    </aside>
  );
}
