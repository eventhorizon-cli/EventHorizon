import ReactMarkdown from "react-markdown";
import { useEffect, useRef } from "react";
import { Loader2, Play, Plus, Settings2, Square } from "lucide-react";
import { ModifiedFilesCard } from "@/components/chat/ModifiedFilesCard";
import { cn } from "@/utils/cn";
import { formatDistanceToNow } from "date-fns";
import type { AgentPhase, AgentRun, AgentSessionDetail, FileChange, LogItem } from "@/types";

type SessionPaneProps = {
  currentSession?: AgentSessionDetail;
  currentRun?: AgentRun;
  availableModels: string[];
  phase: AgentPhase;
  logsCount: number;
  logs: LogItem[];
  changes: FileChange[];
  composerValue: string;
  isSubmitting: boolean;
  isUpdatingSession: boolean;
  onComposerChange: (value: string) => void;
  onComposerSubmit: () => Promise<void> | void;
  onNewChat: () => void;
  onOpenSettings: () => void;
  onCancelRun: () => Promise<void> | void;
  onSelectModel: (model: string) => Promise<void> | void;
  onViewFiles: () => void;
  onViewLogs: () => void;
  onOpenDiff: (change: FileChange) => Promise<void> | void;
};

export function SessionPane({
  currentSession,
  currentRun,
  availableModels,
  phase,
  logsCount,
  logs,
  changes,
  composerValue,
  isSubmitting,
  isUpdatingSession,
  onComposerChange,
  onComposerSubmit,
  onNewChat,
  onOpenSettings,
  onCancelRun,
  onSelectModel,
  onViewFiles,
  onViewLogs,
  onOpenDiff,
}: SessionPaneProps) {
  const hasActiveSession = !!currentSession;
  const hasConfiguredModels = availableModels.length > 0;
  const canSubmit = hasActiveSession && hasConfiguredModels && composerValue.trim().length > 0 && currentRun?.status !== "running";
  const messagesEndRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [currentSession?.messages, composerValue, logs.length]);

  return (
    <main className="flex h-full min-h-0 min-w-0 flex-col overflow-hidden rounded-3xl border border-border/70 bg-background shadow-sm">
      <div className="flex shrink-0 items-center justify-between gap-3 border-b border-border/70 px-4 py-3 sm:px-5">
        <div className="min-w-0">
          <div className="truncate text-sm font-medium">{currentSession?.title ?? "New conversation"}</div>
          <div className="truncate text-xs text-muted-foreground" title={currentSession?.workspaceRoot}>
            {currentSession?.workspaceRoot ?? "No workspace selected"}
          </div>
        </div>

        <div className="flex min-w-0 items-center gap-2">
          <span className="shrink-0 text-xs uppercase tracking-wide text-muted-foreground">Model</span>
          <select
            value={currentSession?.model ?? ""}
            onChange={(event) => void onSelectModel(event.target.value)}
            disabled={!currentSession || currentSession.id.startsWith("draft_") || isUpdatingSession || availableModels.length === 0}
            className="min-w-[180px] max-w-[280px] rounded-xl border border-border bg-background px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary disabled:cursor-not-allowed disabled:opacity-50"
            title={currentSession?.model ?? "No model selected"}
          >
            {availableModels.length === 0 ? <option value="">No configured models</option> : null}
            {availableModels.map((model) => (
              <option key={model} value={model}>
                {model}
              </option>
            ))}
          </select>
        </div>
      </div>

      <div className="min-h-0 flex-1 overflow-y-auto px-4 py-4 sm:px-5">
        {!currentSession ? (
          <div className="flex h-full min-h-[360px] w-full items-center justify-center">
            <div className="w-full rounded-3xl border border-dashed border-border bg-card/80 p-8 text-center shadow-sm">
              <h1 className="text-2xl font-semibold">Create a session before running tasks</h1>
              <p className="mx-auto mt-3 max-w-2xl text-sm text-muted-foreground">
                Pick a workspace directory to start a dedicated session. After that, you can run prompts, inspect changes,
                and keep the conversation history organized.
              </p>

              <div className="mt-6 flex justify-center">
                <button
                  type="button"
                  onClick={onNewChat}
                  className="inline-flex items-center gap-2 rounded-full bg-primary px-5 py-2.5 text-sm font-medium text-primary-foreground shadow-sm transition hover:opacity-90"
                >
                  <Plus className="h-4 w-4" />
                  New Session
                </button>
              </div>

              <div className="mx-auto mt-6 grid max-w-3xl gap-2 text-left text-sm text-muted-foreground sm:grid-cols-2">
                <div className="rounded-2xl bg-muted/60 p-3">Choose the workspace you want the agent to work on.</div>
                <div className="rounded-2xl bg-muted/60 p-3">Keep each task history grouped inside its own session.</div>
              </div>
            </div>
          </div>
        ) : !hasConfiguredModels ? (
          <div className="flex h-full min-h-[360px] w-full items-center justify-center">
            <div className="w-full rounded-3xl border border-dashed border-border bg-card/80 p-8 text-center shadow-sm">
              <h1 className="text-2xl font-semibold">Configure a model before running tasks</h1>
              <p className="mx-auto mt-3 max-w-2xl text-sm text-muted-foreground">
                This session does not have any available models right now. Add or configure a provider model in Settings,
                then come back to continue chatting with the agent.
              </p>

              <div className="mt-6 flex justify-center">
                <button
                  type="button"
                  onClick={onOpenSettings}
                  className="inline-flex items-center gap-2 rounded-full bg-primary px-5 py-2.5 text-sm font-medium text-primary-foreground shadow-sm transition hover:opacity-90"
                >
                  <Settings2 className="h-4 w-4" />
                  Open Settings
                </button>
              </div>

              <div className="mx-auto mt-6 grid max-w-3xl gap-2 text-left text-sm text-muted-foreground sm:grid-cols-2">
                <div className="rounded-2xl bg-muted/60 p-3">Add at least one provider model in the Providers settings.</div>
                <div className="rounded-2xl bg-muted/60 p-3">Once a model is available, Run will be enabled automatically.</div>
              </div>
            </div>
          </div>
        ) : !currentSession.messages.length ? (
          <div className="flex h-full min-h-[360px] w-full items-center justify-center">
            <div className="w-full rounded-3xl border border-dashed border-border bg-card/80 p-8 text-center shadow-sm">
              <h1 className="text-2xl font-semibold">Event Horizon Workbench</h1>
              <p className="mx-auto mt-3 max-w-2xl text-sm text-muted-foreground">
                Ask the agent to inspect, modify, explain, test, or refactor your code.
              </p>

              <div className="mx-auto mt-6 grid max-w-3xl gap-2 text-left text-sm text-muted-foreground sm:grid-cols-2">
                <div className="rounded-2xl bg-muted/60 p-3">Replace CLI/TUI with an AG-UI web interface</div>
                <div className="rounded-2xl bg-muted/60 p-3">Add tests for EventHorizon</div>
                <div className="rounded-2xl bg-muted/60 p-3">Explain why the build is failing</div>
                <div className="rounded-2xl bg-muted/60 p-3">Refactor this service</div>
              </div>
            </div>
          </div>
        ) : (
          <div className="flex w-full flex-col gap-4">
            {currentSession.messages.map((message) => (
              <div key={message.id} className="flex flex-col gap-1">
                {message.role === "assistant" && (
                  <div className="text-sm font-medium text-muted-foreground">🤖 Assistant</div>
                )}
                {message.role === "user" ? (
                  <div className="ml-auto flex flex-col gap-1">
                    <div className="relative rounded-2xl bg-primary px-3 py-1.5 text-primary-foreground shadow-sm">
                      <div className="whitespace-pre-wrap text-sm leading-6">{message.content}</div>
                    </div>
                    <div className="text-[11px] opacity-60 text-right">
                      {formatDistanceToNow(new Date(message.createdAt), { addSuffix: true })}
                      {message.status === "streaming" ? " · streaming" : null}
                    </div>
                  </div>
                ) : (
                  <>
                    <div className="markdown max-w-none">
                      <ReactMarkdown>{message.content}</ReactMarkdown>
                    </div>
                    <div className="text-[11px] opacity-60">
                      {formatDistanceToNow(new Date(message.createdAt), { addSuffix: true })}
                      {message.status === "streaming" ? " · streaming" : null}
                    </div>
                  </>
                )}
              </div>
            ))}
            <div ref={messagesEndRef} />

            {currentRun?.status === "running" ? (
              <div className="w-full rounded-3xl border border-primary/20 bg-card p-4 shadow-sm">
                <div className="flex items-center gap-2 text-sm font-medium">
                  <Loader2 className="h-4 w-4 animate-spin text-primary" />
                  Agent is working
                </div>

                <div className="mt-3 grid gap-2 text-sm text-muted-foreground">
                  <div>• Current phase: {phase}</div>
                  <div>• Task: {currentRun.task}</div>
                  <div>• Logs: {logsCount}</div>
                </div>

                <div className="mt-4 flex flex-wrap gap-2">
                  <button
                    type="button"
                    onClick={onViewFiles}
                    className="rounded-xl border border-border px-3 py-1.5 text-xs transition hover:bg-muted"
                  >
                    View files
                  </button>
                  <button
                    type="button"
                    onClick={onViewLogs}
                    className="rounded-xl border border-border px-3 py-1.5 text-xs transition hover:bg-muted"
                  >
                    View logs
                  </button>
                </div>
              </div>
            ) : null}

            {currentRun && changes.length > 0 ? (
              <ModifiedFilesCard
                runId={currentRun.id}
                files={changes}
                onViewFiles={onViewFiles}
                onViewDiff={(path) => {
                  const target = changes.find((change) => change.path === path);
                  if (target) {
                    void onOpenDiff(target);
                  }
                }}
              />
            ) : null}
          </div>
        )}
      </div>

      <div className="shrink-0 border-t border-border/70 bg-card/95 px-4 py-4 sm:px-5">
        <div className="w-full rounded-3xl border border-border bg-background p-3 shadow-sm transition focus-within:border-primary/60 focus-within:ring-4 focus-within:ring-primary/10">
          <textarea
            value={composerValue}
            onChange={(event) => onComposerChange(event.target.value)}
            disabled={!hasActiveSession || !hasConfiguredModels}
            onKeyDown={(event) => {
              if (!hasActiveSession || !hasConfiguredModels) {
                return;
              }

              if (event.nativeEvent.isComposing || event.key !== "Enter" || event.altKey) {
                return;
              }

              event.preventDefault();
              void onComposerSubmit();
            }}
            placeholder={
              !hasActiveSession
                ? "Create a new session to start chatting with the agent..."
                : !hasConfiguredModels
                  ? "Configure a model in Settings to enable Run..."
                  : "Ask the agent to change, explain, test, or refactor your code..."
            }
            className="min-h-28 w-full resize-none bg-transparent text-sm leading-6 outline-none placeholder:text-muted-foreground disabled:cursor-not-allowed disabled:opacity-60"
          />

          <div className="mt-3 flex items-center justify-between gap-3">
            <div className="text-xs text-muted-foreground">
              {!hasActiveSession
                ? "Create a session first to enable Run"
                : !hasConfiguredModels
                  ? "Configure a model first to enable Run"
                  : "Enter to send · Alt + Enter for newline"}
            </div>

            <div className="flex items-center gap-2">

              {currentRun?.status === "running" ? (
                <button
                  type="button"
                  onClick={() => void onCancelRun()}
                  className="inline-flex items-center gap-2 rounded-full border border-border bg-background px-4 py-2 text-sm font-medium text-muted-foreground shadow-sm transition hover:bg-muted hover:text-foreground"
                >
                  <Square className="h-4 w-4" />
                  Cancel
                </button>
              ) : null}

              <button
                type="button"
                onClick={() => void onComposerSubmit()}
                disabled={!canSubmit || isSubmitting}
                className={cn(
                  "inline-flex items-center gap-2 rounded-full px-4 py-2 text-sm font-medium shadow-sm transition",
                  canSubmit && !isSubmitting
                    ? "bg-primary text-primary-foreground hover:opacity-90"
                    : "bg-muted text-muted-foreground cursor-not-allowed",
                )}
              >
                {isSubmitting ? <Loader2 className="h-4 w-4 animate-spin" /> : <Play className="h-4 w-4" />}
                Run
              </button>
            </div>
          </div>
        </div>
      </div>
    </main>
  );
}
