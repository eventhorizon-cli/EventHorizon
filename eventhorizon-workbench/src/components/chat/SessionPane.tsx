import ReactMarkdown from "react-markdown";
import { useEffect, useMemo, useRef, useState } from "react";
import { ChevronDown, ChevronRight, Loader2, Play, Plus, Settings2, Square } from "lucide-react";
import { ModifiedFilesCard } from "@/components/chat/ModifiedFilesCard";
import { cn } from "@/utils/cn";
import {
  buildToolCallTimeline,
  formatToolCallSignature,
  getToolCallStatusIcon,
  type ToolCallTimelineItem,
} from "@/utils/toolCalls";
import { formatDistanceToNow } from "date-fns";
import type { AgentRun, AgentSessionDetail, FileChange, LogItem } from "@/types";

type SessionPaneProps = {
  currentSession?: AgentSessionDetail;
  currentRun?: AgentRun;
  availableModels: string[];
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
  onOpenDiff: (change: FileChange) => Promise<void> | void;
};

function isNearBottom(element: HTMLElement) {
  return element.scrollHeight - element.scrollTop - element.clientHeight < 96;
}

function ToolCallDetailSection({ label, value, tone = "default" }: { label: string; value?: string; tone?: "default" | "error" }) {
  if (!value) {
    return null;
  }

  return (
    <div className="min-w-0 max-w-full space-y-1">
      <div className="text-[11px] font-medium uppercase tracking-wide text-muted-foreground">{label}</div>
      <pre
        className={cn(
          "min-w-0 max-w-full overflow-x-auto rounded-xl border border-border/70 bg-muted/40 px-3 py-2 font-mono text-xs leading-5 whitespace-pre-wrap break-words",
          tone === "error" && "border-red-200/80 bg-red-50/70 text-red-700 dark:border-red-500/40 dark:bg-red-950/30 dark:text-red-200",
        )}
      >
        {value}
      </pre>
    </div>
  );
}

function ToolCallActivity({ toolCalls }: { toolCalls: ToolCallTimelineItem[] }) {
  const summaryScrollRef = useRef<HTMLDivElement>(null);
  const detailScrollRef = useRef<HTMLDivElement>(null);
  const latestToolCallRef = useRef<HTMLDivElement>(null);
  const shouldStickToBottomRef = useRef(true);
  const [expanded, setExpanded] = useState(false);
  const [expandedToolCallIds, setExpandedToolCallIds] = useState<Record<string, boolean>>({});

  useEffect(() => {
    const element = summaryScrollRef.current;
    if (expanded || !element) {
      return;
    }

    element.scrollTo({ top: element.scrollHeight, behavior: "auto" });
  }, [expanded, toolCalls]);

  useEffect(() => {
    if (!expanded || !latestToolCallRef.current) {
      return;
    }

    latestToolCallRef.current.scrollIntoView({ block: "nearest", behavior: "auto" });
  }, [expanded, toolCalls]);

  if (toolCalls.length === 0) {
    return null;
  }

  return (
    <div className="mt-4 min-w-0 max-w-full rounded-2xl border border-border/70 bg-background/70 p-3">
      <button
        type="button"
        onClick={() => {
          setExpanded((current) => {
            const next = !current;
            if (next) {
              shouldStickToBottomRef.current = true;
            }
            return next;
          });
        }}
        aria-expanded={expanded}
        className="flex w-full min-w-0 max-w-full items-start justify-between gap-3 text-left"
      >
        <div className="min-w-0 flex-1">
          <div className="flex items-center gap-2 text-xs uppercase tracking-wide text-muted-foreground">
            <span>Tool activity</span>
            <span className="rounded-full bg-muted px-2 py-0.5 normal-case text-[11px]">{toolCalls.length} call(s)</span>
          </div>

          {!expanded ? (
            <div className="mt-2 min-w-0 overflow-hidden rounded-2xl border border-border bg-card/90 px-3 py-2 shadow-sm">
              <div ref={summaryScrollRef} className="h-6 min-w-0 overflow-hidden" aria-live="polite">
                <div className="min-w-0 space-y-2">
                  {toolCalls.map((toolCall) => {
                    const signature = formatToolCallSignature(toolCall.name, toolCall.arguments);

                    return (
                      <div key={toolCall.id} className="flex h-6 min-w-0 items-center gap-2">
                        <span
                          aria-hidden="true"
                          className={cn(
                            "shrink-0",
                            toolCall.status === "failed"
                              ? "text-red-600 dark:text-red-300"
                              : toolCall.status === "completed"
                                ? "text-emerald-700 dark:text-emerald-300"
                                : "text-primary",
                          )}
                        >
                          {getToolCallStatusIcon(toolCall.status)}
                        </span>
                        <span className="sr-only">{toolCall.status}</span>
                        <span className="min-w-0 flex-1 truncate font-mono text-sm leading-6 whitespace-nowrap text-foreground" title={signature}>
                          {signature}
                        </span>
                        <span className="shrink-0 text-[11px] uppercase tracking-wide text-muted-foreground">{toolCall.status}</span>
                      </div>
                    );
                  })}
                </div>
              </div>
            </div>
          ) : null}
        </div>

        {expanded ? (
          <ChevronDown className="mt-1 h-5 w-5 shrink-0 text-muted-foreground" />
        ) : (
          <ChevronRight className="mt-1 h-5 w-5 shrink-0 text-muted-foreground" />
        )}
      </button>

      {expanded ? (
        <div
          ref={detailScrollRef}
          onScroll={(event) => {
            shouldStickToBottomRef.current = isNearBottom(event.currentTarget);
          }}
          className="mt-3 grid min-w-0 max-w-full max-h-72 gap-3 overflow-x-hidden overflow-y-auto pr-1"
        >
          {toolCalls.map((toolCall, index) => {
            const signature = formatToolCallSignature(toolCall.name, toolCall.arguments);
            const isExpanded = !!expandedToolCallIds[toolCall.id];
            const isLatest = index === toolCalls.length - 1;

            return (
              <div
                key={toolCall.id}
                ref={isLatest ? latestToolCallRef : undefined}
                className="min-w-0 max-w-full overflow-hidden rounded-2xl border border-border bg-card/90 px-3 py-2 shadow-sm"
              >
                <button
                  type="button"
                  onClick={() => {
                    setExpandedToolCallIds((current) => ({
                      ...current,
                      [toolCall.id]: !current[toolCall.id],
                    }));
                  }}
                  aria-expanded={isExpanded}
                  className="flex min-w-0 w-full max-w-full items-center gap-2 text-left"
                >
                  <span
                    aria-hidden="true"
                    className={cn(
                      "shrink-0",
                      toolCall.status === "failed"
                        ? "text-red-600 dark:text-red-300"
                        : toolCall.status === "completed"
                          ? "text-emerald-700 dark:text-emerald-300"
                          : "text-primary",
                    )}
                  >
                    {getToolCallStatusIcon(toolCall.status)}
                  </span>
                  <span className="sr-only">{toolCall.status}</span>
                  <span className="min-w-0 flex-1 truncate font-mono text-sm leading-6 whitespace-nowrap text-foreground" title={signature}>
                    {signature}
                  </span>
                  <span className="shrink-0 text-[11px] uppercase tracking-wide text-muted-foreground">{toolCall.status}</span>
                  {isExpanded ? (
                    <ChevronDown className="h-4 w-4 shrink-0 text-muted-foreground" />
                  ) : (
                    <ChevronRight className="h-4 w-4 shrink-0 text-muted-foreground" />
                  )}
                </button>

                {isExpanded ? (
                  <div className="mt-3 min-w-0 max-w-full space-y-3 overflow-hidden border-t border-border/70 pt-3">
                    <ToolCallDetailSection label="Call" value={signature} />
                    <ToolCallDetailSection label="Arguments" value={toolCall.arguments} />
                    <ToolCallDetailSection label="Result" value={toolCall.result} />
                    <ToolCallDetailSection label="Error" value={toolCall.error} tone="error" />
                    <div className="flex flex-wrap gap-x-4 gap-y-1 text-[11px] text-muted-foreground">
                      <span>Started: {new Date(toolCall.startedAt).toLocaleString()}</span>
                      <span>Updated: {new Date(toolCall.updatedAt).toLocaleString()}</span>
                    </div>
                  </div>
                ) : null}
              </div>
            );
          })}
        </div>
      ) : null}
    </div>
  );
}

export function SessionPane({
  currentSession,
  currentRun,
  availableModels,
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
  onOpenDiff,
}: SessionPaneProps) {
  const hasActiveSession = !!currentSession;
  const hasConfiguredModels = availableModels.length > 0;
  const canSubmit = hasActiveSession && hasConfiguredModels && composerValue.trim().length > 0 && currentRun?.status !== "running";
  const isRunActive = currentRun?.status === "running";
  const messagesScrollRef = useRef<HTMLDivElement>(null);
  const shouldStickToBottomRef = useRef(true);
  const toolCalls = useMemo(() => buildToolCallTimeline(logs, currentRun?.id), [logs, currentRun?.id]);
  const hasStreamingAssistantMessage = useMemo(
    () => currentSession?.messages.some((message) => message.role === "assistant" && message.status === "streaming") ?? false,
    [currentSession?.messages],
  );

  useEffect(() => {
    const element = messagesScrollRef.current;
    if (!element || !shouldStickToBottomRef.current) {
      return;
    }

    element.scrollTo({ top: element.scrollHeight, behavior: "auto" });
  }, [currentSession?.messages, currentRun?.status, toolCalls.length]);

  useEffect(() => {
    shouldStickToBottomRef.current = true;
  }, [currentSession?.id]);

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

      <div
        ref={messagesScrollRef}
        onScroll={(event) => {
          shouldStickToBottomRef.current = isNearBottom(event.currentTarget);
        }}
        className="min-h-0 flex-1 overflow-y-auto px-4 py-4 sm:px-5"
      >
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
                  <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground">
                    <span>🤖 Assistant</span>
                    {message.status === "streaming" ? <Loader2 className="h-4 w-4 animate-spin text-primary" /> : null}
                  </div>
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
            {isRunActive && !hasStreamingAssistantMessage ? (
              <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground">
                <span>🤖 Assistant</span>
                <Loader2 className="h-4 w-4 animate-spin text-primary" />
              </div>
            ) : null}

            {currentRun && toolCalls.length > 0 ? <ToolCallActivity toolCalls={toolCalls} /> : null}

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
            className="min-h-16 w-full resize-none bg-transparent text-sm leading-6 outline-none placeholder:text-muted-foreground disabled:cursor-not-allowed disabled:opacity-60"
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
