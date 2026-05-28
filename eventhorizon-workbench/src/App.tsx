import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import ReactMarkdown from "react-markdown";
import { formatDistanceToNow } from "date-fns";
import {
  Loader2,
  PanelLeftClose,
  PanelLeftOpen,
  PanelRightClose,
  Play,
  Square,
  Wifi,
  WifiOff,
} from "lucide-react";
import { Panel, PanelGroup, PanelResizeHandle } from "react-resizable-panels";
import { subscribeRunEvents } from "@/api/aguiClient";
import { getChanges, getFileDiff } from "@/api/diffApi";
import { cancelRun, createRun, getRun } from "@/api/runsApi";
import { createSession, getSession, getSessions } from "@/api/sessionsApi";
import { DiffViewer } from "@/components/diff/DiffViewer";
import { useWorkbenchStore } from "@/store/workbenchStore";
import { ThemeToggle } from "@/theme/ThemeToggle";
import { cn } from "@/utils/cn";
import { buildTemporarySessionTitle } from "@/utils/sessionTitle";
import type { AgentEvent, AgentRun, AgentSessionDetail, FileChange } from "@/types";

const rightPaneKey = "event-horizon-workbench-right-pane-collapsed";
const leftPaneKey = "event-horizon-workbench-left-pane-collapsed";
const compactLayoutQuery = "(max-width: 1180px)";

function mapPhase(event: AgentEvent) {
  switch (event.type) {
    case "runStarted":
      return "understanding" as const;
    case "plan.updated":
      return "planning" as const;
    case "toolCallStart":
      return "editing" as const;
    case "command.started":
    case "test.started":
      return "validating" as const;
    case "runFinished":
      return "completed" as const;
    case "runError":
      return "failed" as const;
    case "runCancelled":
      return "cancelled" as const;
    default:
      return undefined;
  }
}

function createDraftSession(task: string): AgentSessionDetail {
  const id = `draft_${crypto.randomUUID()}`;

  return {
    id,
    title: buildTemporarySessionTitle(task),
    status: "idle",
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    isTitleGenerated: false,
    messages: [],
  };
}

function eventSummary(event: AgentEvent) {
  if (event.text) return event.text;
  if (event.error) return event.error;

  if (typeof event.metadata === "object" && event.metadata && "toolCall" in (event.metadata as object)) {
    return JSON.stringify(event.metadata);
  }

  return event.type;
}

export default function App() {
  const {
    sessions,
    currentSession,
    currentRun,
    phase,
    connectionStatus,
    contextView,
    rightPaneCollapsed,
    selectedFile,
    changes,
    currentDiff,
    logs,
    themeMode,
    setSessions,
    setCurrentSession,
    setCurrentRun,
    setPhase,
    setConnectionStatus,
    setContextView,
    toggleRightPane,
    setSelectedFile,
    setChanges,
    setCurrentDiff,
    addLog,
    appendAssistantDelta,
    finishAssistantMessage,
    addUserMessage,
  } = useWorkbenchStore();

  const [composerValue, setComposerValue] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [leftPaneCollapsed, setLeftPaneCollapsed] = useState(false);
  const [isCompactLayout, setIsCompactLayout] = useState(false);

  const eventSubscriptionRef = useRef<(() => void) | null>(null);
  const currentSessionId = currentSession?.id;

  const resolvedTheme = useMemo(() => {
    if (themeMode === "system") {
      return document.documentElement.classList.contains("dark") ? "dark" : "light";
    }

    return themeMode;
  }, [themeMode]);

  useEffect(() => {
    const collapsed = localStorage.getItem(rightPaneKey);

    if (collapsed === "true" && !rightPaneCollapsed) {
      toggleRightPane();
    }
  }, []);

  useEffect(() => {
    localStorage.setItem(rightPaneKey, rightPaneCollapsed ? "true" : "false");
  }, [rightPaneCollapsed]);

  useEffect(() => {
    const stored = localStorage.getItem(leftPaneKey);

    if (stored === "true") {
      setLeftPaneCollapsed(true);
    }
  }, []);

  useEffect(() => {
    const mediaQuery = window.matchMedia(compactLayoutQuery);

    const syncLayout = () => {
      const compact = mediaQuery.matches;
      setIsCompactLayout(compact);

      if (compact) {
        setLeftPaneCollapsed(true);
        return;
      }

      const stored = localStorage.getItem(leftPaneKey);
      setLeftPaneCollapsed(stored === "true");
    };

    syncLayout();
    mediaQuery.addEventListener("change", syncLayout);

    return () => {
      mediaQuery.removeEventListener("change", syncLayout);
    };
  }, []);

  const toggleLeftPaneCollapsed = useCallback(() => {
    setLeftPaneCollapsed((previous) => {
      const next = !previous;
      localStorage.setItem(leftPaneKey, next ? "true" : "false");
      return next;
    });
  }, []);

  const openSession = useCallback(
    async (sessionId: string) => {
      const detail = await getSession(sessionId);
      setCurrentSession(detail);
      setCurrentDiff(undefined);
      setSelectedFile(undefined);
      setContextView("overview");

      if (detail.lastRunId) {
        const run = await getRun(detail.lastRunId);
        setCurrentRun(run);

        if (run.status !== "running") {
          try {
            setChanges(await getChanges(run.id));
          } catch {
            setChanges([]);
          }
        }

        if (run.status === "running") {
          subscribeToRun(run, detail.id);
        }
      } else {
        setCurrentRun(undefined);
        setChanges([]);
      }
    },
    [setChanges, setContextView, setCurrentDiff, setCurrentRun, setCurrentSession, setSelectedFile],
  );

  const refreshSessions = useCallback(async () => {
    const list = await getSessions();
    setSessions(list);

    if (!currentSessionId && list[0]) {
      await openSession(list[0].id);
    }
  }, [currentSessionId, openSession, setSessions]);

  useEffect(() => {
    void refreshSessions();
  }, [refreshSessions]);

  useEffect(() => {
    return () => {
      eventSubscriptionRef.current?.();
    };
  }, []);

  function subscribeToRun(run: AgentRun, sessionId: string) {
    eventSubscriptionRef.current?.();
    setConnectionStatus("connecting");

    eventSubscriptionRef.current = subscribeRunEvents(run.id, {
      onOpen: () => setConnectionStatus("connected"),
      onClose: () => setConnectionStatus("disconnected"),
      onError: () => setConnectionStatus("reconnecting"),
      onEvent: async (event) => {
        addLog(event);

        const nextPhase = mapPhase(event);
        if (nextPhase) {
          setPhase(nextPhase);
        }

        switch (event.type) {
          case "textMessageContent":
            appendAssistantDelta(sessionId, event.delta ?? "");
            break;

          case "textMessageEnd":
            finishAssistantMessage(sessionId, event.text ?? "");
            break;

          case "runFinished": {
            const updatedRun = await getRun(run.id);
            setCurrentRun(updatedRun);
            setPhase("completed");
            setChanges(await getChanges(run.id));
            await refreshSessions();

            if (currentSessionId === sessionId) {
              await openSession(sessionId);
            }

            break;
          }

          case "runError":
            setPhase("failed");
            await refreshSessions();
            break;

          case "runCancelled":
            setPhase("cancelled");
            await refreshSessions();
            break;

          case "file.created":
          case "file.modified":
          case "file.deleted":
          case "diff.generated":
            setContextView("files");

            if (run.id) {
              try {
                setChanges(await getChanges(run.id));
              } catch {
                return;
              }
            }

            break;

          default:
            break;
        }
      },
    });
  }

  async function handleSubmit() {
    const task = composerValue.trim();

    if (!task || isSubmitting) {
      return;
    }

    setIsSubmitting(true);

    try {
      let activeSession = currentSession;

      if (!activeSession) {
        activeSession = createDraftSession(task);
        setCurrentSession(activeSession);
      }

      if (activeSession.id.startsWith("draft_")) {
        const created = await createSession(task);
        const detail = await getSession(created.id);
        setCurrentSession(detail);
        activeSession = detail;
        await refreshSessions();
      }

      addUserMessage(activeSession.id, task);

      const run = await createRun({ sessionId: activeSession.id, task });
      setCurrentRun(run);
      setPhase("understanding");
      setConnectionStatus("connecting");
      subscribeToRun(run, activeSession.id);
      setComposerValue("");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function handleCancel() {
    if (!currentRun) {
      return;
    }

    await cancelRun(currentRun.id);
  }

  async function openDiff(change: FileChange) {
    if (!currentRun) {
      return;
    }

    setSelectedFile(change.path);
    setContextView("diff");
    setCurrentDiff(await getFileDiff(currentRun.id, change.path));
  }

  const statusLabel = currentRun?.status ?? "idle";

  const canSubmit =
    composerValue.trim().length > 0 &&
    connectionStatus !== "disconnected" &&
    currentRun?.status !== "running";

  return (
    <div className="flex h-screen min-h-0 flex-col overflow-hidden bg-muted/40 p-3 text-foreground">
      <header className="shrink-0 px-1 pb-3 pt-1">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div className="min-w-0">
            <div className="truncate text-lg font-semibold">Event Horizon Workbench</div>
            <div className="truncate text-xs text-muted-foreground">
              {statusLabel} · {phase} ·{" "}
              {currentRun?.createdAt
                ? formatDistanceToNow(new Date(currentRun.createdAt), { addSuffix: true })
                : "Idle"}
            </div>
          </div>

          <div className="flex items-center gap-3">
            <div className="inline-flex items-center gap-2 rounded-full bg-background/60 px-3 py-1.5 text-xs text-muted-foreground shadow-sm ring-1 ring-border/40">
              {connectionStatus === "connected" ? (
                <Wifi className="h-3.5 w-3.5 text-emerald-500" />
              ) : (
                <WifiOff className="h-3.5 w-3.5 text-amber-500" />
              )}
              <span>{connectionStatus}</span>
            </div>

            {currentRun?.status === "running" ? (
              <button
                type="button"
                onClick={handleCancel}
                className="inline-flex items-center gap-2 rounded-full bg-destructive px-3 py-2 text-xs font-medium text-destructive-foreground shadow-sm transition hover:opacity-90"
              >
                <Square className="h-3.5 w-3.5" />
                Cancel
              </button>
            ) : null}

            <ThemeToggle />

            <button
              type="button"
              onClick={toggleRightPane}
              className="rounded-2xl bg-background/60 p-2 text-muted-foreground shadow-sm ring-1 ring-border/40 transition hover:bg-background hover:text-foreground"
              title={rightPaneCollapsed ? "Expand details" : "Collapse details"}
            >
              {rightPaneCollapsed ? <PanelRightOpenIcon /> : <PanelRightClose className="h-4 w-4" />}
            </button>
          </div>
        </div>
      </header>

      <div className="flex min-h-0 flex-1 gap-3 overflow-hidden">
        <aside
          className={cn(
            "hidden min-h-0 shrink-0 flex-col overflow-hidden rounded-3xl border border-border/70 bg-card/95 shadow-sm transition-[width] duration-300 ease-out md:flex",
            leftPaneCollapsed ? "w-[72px]" : "w-[280px]",
          )}
        >
          <div
            className={cn(
              "flex shrink-0 border-b border-border/70 p-3",
              leftPaneCollapsed ? "flex-col items-center gap-2" : "items-center justify-between gap-3",
            )}
          >
            {!leftPaneCollapsed ? (
              <div className="min-w-0">
                <div className="text-sm font-semibold">Sessions</div>
                <div className="text-xs text-muted-foreground">
                  {isCompactLayout ? "Auto compact" : "History"}
                </div>
              </div>
            ) : null}

            <div
              className={cn(
                "flex shrink-0 gap-2",
                leftPaneCollapsed ? "flex-col items-center" : "items-center",
              )}
            >
              <button
                type="button"
                onClick={toggleLeftPaneCollapsed}
                className={cn(
                  "inline-flex items-center justify-center rounded-2xl bg-background/80 text-muted-foreground shadow-sm ring-1 ring-border/60 transition hover:bg-muted hover:text-foreground",
                  leftPaneCollapsed ? "h-10 w-10" : "h-9 w-9",
                )}
                title={leftPaneCollapsed ? "Expand sessions" : "Collapse sessions"}
              >
                {leftPaneCollapsed ? (
                  <PanelLeftOpen className="h-4 w-4" />
                ) : (
                  <PanelLeftClose className="h-4 w-4" />
                )}
              </button>

              <button
                type="button"
                onClick={() => setCurrentSession(undefined)}
                className={cn(
                  "inline-flex items-center justify-center rounded-2xl bg-primary text-primary-foreground shadow-sm transition hover:opacity-90",
                  leftPaneCollapsed ? "h-10 w-10 text-lg" : "px-3 py-2 text-xs font-medium",
                )}
                title="New Chat"
              >
                {leftPaneCollapsed ? "+" : "New Chat"}
              </button>
            </div>
          </div>

          <div className="min-h-0 flex-1 overflow-y-auto p-2">
            {sessions.length === 0 ? (
              <div
                className={cn(
                  "rounded-2xl border border-dashed border-border p-4 text-sm text-muted-foreground",
                  leftPaneCollapsed && "p-2 text-center text-xs",
                )}
              >
                {leftPaneCollapsed ? "Empty" : "No sessions yet."}
              </div>
            ) : null}

            <div className="space-y-1.5">
              {sessions.map((session) => {
                const active = currentSession?.id === session.id;
                const initial = session.title?.trim()?.[0]?.toUpperCase() || "S";

                return (
                  <button
                    key={session.id}
                    type="button"
                    title={session.title}
                    onClick={() => void openSession(session.id)}
                    className={cn(
                      "group w-full rounded-2xl border text-left transition-all",
                      active
                        ? "border-primary bg-primary/10 shadow-sm"
                        : "border-transparent hover:border-border hover:bg-muted/70",
                      leftPaneCollapsed ? "flex h-12 items-center justify-center px-0 py-0" : "px-3 py-3",
                    )}
                  >
                    {leftPaneCollapsed ? (
                      <div
                        className={cn(
                          "flex h-8 w-8 items-center justify-center rounded-xl text-xs font-semibold",
                          active
                            ? "bg-primary text-primary-foreground"
                            : "bg-muted text-muted-foreground group-hover:text-foreground",
                        )}
                      >
                        {initial}
                      </div>
                    ) : (
                      <>
                        <div className="truncate text-sm font-medium">{session.title}</div>
                        <div className="mt-1 truncate text-xs text-muted-foreground">
                          {formatDistanceToNow(new Date(session.updatedAt), { addSuffix: true })} ·{" "}
                          {session.status}
                        </div>
                      </>
                    )}
                  </button>
                );
              })}
            </div>
          </div>
        </aside>

        <PanelGroup direction="horizontal" className="min-h-0 flex-1 overflow-hidden">
          <Panel defaultSize={rightPaneCollapsed ? 100 : 64} minSize={45} className="min-h-0 min-w-0">
            <main className="flex h-full min-h-0 min-w-0 flex-col overflow-hidden rounded-3xl border border-border/70 bg-background shadow-sm">
              <div className="min-h-0 flex-1 overflow-y-auto px-4 py-4 sm:px-5">
                {!currentSession?.messages.length ? (
                  <div className="flex h-full min-h-[360px] w-full items-center justify-center">
                    <div className="w-full rounded-3xl border border-dashed border-border bg-card/80 p-8 text-center shadow-sm">
                      <h1 className="text-2xl font-semibold">Event Horizon Workbench</h1>
                      <p className="mx-auto mt-3 max-w-2xl text-sm text-muted-foreground">
                        Ask the agent to inspect, modify, explain, test, or refactor your code.
                      </p>

                      <div className="mx-auto mt-6 grid max-w-3xl gap-2 text-left text-sm text-muted-foreground sm:grid-cols-2">
                        <div className="rounded-2xl bg-muted/60 p-3">
                          Replace CLI/TUI with an AG-UI web interface
                        </div>
                        <div className="rounded-2xl bg-muted/60 p-3">Add tests for EventHorizon</div>
                        <div className="rounded-2xl bg-muted/60 p-3">Explain why the build is failing</div>
                        <div className="rounded-2xl bg-muted/60 p-3">Refactor this service</div>
                      </div>
                    </div>
                  </div>
                ) : (
                  <div className="flex w-full flex-col gap-4">
                    {currentSession.messages.map((message) => (
                      <div
                        key={message.id}
                        className={cn(
                          "rounded-3xl border p-4 shadow-sm",
                          message.role === "user"
                            ? "ml-auto max-w-[min(780px,88%)] border-primary/20 bg-primary text-primary-foreground"
                            : "w-full border-border/70 bg-card",
                        )}
                      >
                        <div className="mb-2 text-[11px] uppercase tracking-wide opacity-70">
                          {message.role}
                        </div>

                        {message.role === "assistant" ? (
                          <div className="markdown max-w-none">
                            <ReactMarkdown>{message.content}</ReactMarkdown>
                          </div>
                        ) : (
                          <div className="whitespace-pre-wrap text-sm leading-7">{message.content}</div>
                        )}

                        <div className="mt-3 text-[11px] opacity-60">
                          {formatDistanceToNow(new Date(message.createdAt), { addSuffix: true })}
                          {message.status === "streaming" ? " · streaming" : null}
                        </div>
                      </div>
                    ))}

                    {currentRun?.status === "running" ? (
                      <div className="w-full rounded-3xl border border-primary/20 bg-card p-4 shadow-sm">
                        <div className="flex items-center gap-2 text-sm font-medium">
                          <Loader2 className="h-4 w-4 animate-spin text-primary" />
                          Agent is working
                        </div>

                        <div className="mt-3 grid gap-2 text-sm text-muted-foreground">
                          <div>• Current phase: {phase}</div>
                          <div>• Task: {currentRun.task}</div>
                          <div>• Logs: {logs.length}</div>
                        </div>

                        <div className="mt-4 flex flex-wrap gap-2">
                          <button
                            type="button"
                            onClick={() => setContextView("files")}
                            className="rounded-xl border border-border px-3 py-1.5 text-xs transition hover:bg-muted"
                          >
                            View files
                          </button>
                          <button
                            type="button"
                            onClick={() => setContextView("logs")}
                            className="rounded-xl border border-border px-3 py-1.5 text-xs transition hover:bg-muted"
                          >
                            View logs
                          </button>
                        </div>
                      </div>
                    ) : null}
                  </div>
                )}
              </div>

              <div className="shrink-0 border-t border-border/70 bg-card/95 px-4 py-4 sm:px-5">
                <div className="w-full">
                  <div className="rounded-3xl border border-border bg-background p-3 shadow-sm transition focus-within:border-primary/60 focus-within:ring-4 focus-within:ring-primary/10">
                    <textarea
                      value={composerValue}
                      onChange={(event) => setComposerValue(event.target.value)}
                      onKeyDown={(event) => {
                        if (event.nativeEvent.isComposing) return;
                        if (event.key !== "Enter") return;

                        if (event.altKey) {
                          return;
                        }

                        event.preventDefault();
                        void handleSubmit();
                      }}
                      placeholder="Ask the agent to change, explain, test, or refactor your code..."
                      className="min-h-28 w-full resize-none bg-transparent text-sm leading-6 outline-none placeholder:text-muted-foreground"
                    />

                    <div className="mt-3 flex items-center justify-between">
                      <div className="text-xs text-muted-foreground">Enter to send · Alt + Enter for newline</div>

                      <button
                        type="button"
                        onClick={() => void handleSubmit()}
                        disabled={!canSubmit || isSubmitting}
                        className="inline-flex items-center gap-2 rounded-full bg-primary px-4 py-2 text-sm font-medium text-primary-foreground shadow-sm transition hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-50"
                      >
                        {isSubmitting ? <Loader2 className="h-4 w-4 animate-spin" /> : <Play className="h-4 w-4" />}
                        Run
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            </main>
          </Panel>

          {!rightPaneCollapsed ? (
            <PanelResizeHandle className="flex w-3 items-center justify-center bg-transparent">
              <div className="h-12 w-1 rounded-full bg-border transition hover:bg-primary/40" />
            </PanelResizeHandle>
          ) : null}

          {!rightPaneCollapsed ? (
            <Panel defaultSize={36} minSize={26} className="min-h-0 min-w-0">
              <aside className="flex h-full min-h-0 min-w-0 flex-col overflow-hidden rounded-3xl border border-border/70 bg-card/95 shadow-sm">
                <div className="flex shrink-0 items-center justify-between border-b border-border/70 px-4 py-3">
                  <div className="flex gap-1 rounded-full bg-muted p-1 text-xs">
                    {(["overview", "files", "diff", "logs"] as const).map((view) => (
                      <button
                        key={view}
                        type="button"
                        onClick={() => setContextView(view)}
                        className={cn(
                          "rounded-full px-3 py-1.5 capitalize transition",
                          contextView === view
                            ? "bg-card text-foreground shadow-sm"
                            : "text-muted-foreground hover:text-foreground",
                        )}
                      >
                        {view}
                      </button>
                    ))}
                  </div>

                  <button
                    type="button"
                    onClick={toggleRightPane}
                    className="rounded-xl p-2 text-muted-foreground transition hover:bg-muted hover:text-foreground"
                  >
                    <PanelRightClose className="h-4 w-4" />
                  </button>
                </div>

                <div className="min-h-0 flex-1 overflow-y-auto p-4">
                  {contextView === "overview" ? (
                    <div className="space-y-4 text-sm">
                      <section className="rounded-2xl border border-border bg-background/50 p-4">
                        <div className="text-xs uppercase tracking-wide text-muted-foreground">Current task</div>
                        <div className="mt-2 font-medium">{currentRun?.task ?? "No active run"}</div>
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

                  {contextView === "files" ? (
                    <div className="space-y-2">
                      {changes.length === 0 ? (
                        <div className="rounded-2xl border border-dashed border-border p-4 text-sm text-muted-foreground">
                          No file changes yet.
                        </div>
                      ) : null}

                      {changes.map((change) => (
                        <button
                          key={change.path}
                          type="button"
                          onClick={() => void openDiff(change)}
                          className={cn(
                            "flex w-full items-center justify-between gap-3 rounded-2xl border px-3 py-3 text-left transition hover:bg-muted",
                            selectedFile === change.path
                              ? "border-primary bg-primary/10"
                              : "border-border bg-background/50",
                          )}
                        >
                          <div className="min-w-0">
                            <div className="truncate text-sm font-medium">{change.path}</div>
                            <div className="text-xs text-muted-foreground">{change.status}</div>
                          </div>
                          <div className="shrink-0 text-xs text-muted-foreground">
                            +{change.additions ?? 0} / -{change.deletions ?? 0}
                          </div>
                        </button>
                      ))}
                    </div>
                  ) : null}

                  {contextView === "diff" ? (
                    currentDiff ? (
                      <div className="h-full min-h-[360px] overflow-hidden rounded-2xl border border-border">
                        <DiffViewer
                          {...currentDiff}
                          theme={resolvedTheme === "dark" ? "dark" : "light"}
                          onBack={() => setContextView("files")}
                        />
                      </div>
                    ) : (
                      <div className="rounded-2xl border border-dashed border-border p-4 text-sm text-muted-foreground">
                        Select a changed file to inspect the diff.
                      </div>
                    )
                  ) : null}

                  {contextView === "logs" ? (
                    <div className="space-y-2">
                      {logs.length === 0 ? (
                        <div className="rounded-2xl border border-dashed border-border p-4 text-sm text-muted-foreground">
                          No logs yet.
                        </div>
                      ) : null}

                      {logs.map((log) => (
                        <div key={log.id} className="rounded-2xl border border-border bg-background/50 p-3">
                          <div className="flex items-center justify-between gap-3 text-xs text-muted-foreground">
                            <span>{log.type}</span>
                            <span>{formatDistanceToNow(new Date(log.timestamp), { addSuffix: true })}</span>
                          </div>

                          <div className="mt-2 text-sm">{log.summary || eventSummary(log.event)}</div>

                          <details className="mt-3 text-xs text-muted-foreground">
                            <summary className="cursor-pointer">Raw JSON</summary>
                            <pre className="mt-2 overflow-x-auto rounded-xl bg-muted p-3 text-xs">
                              {JSON.stringify(log.event, null, 2)}
                            </pre>
                          </details>
                        </div>
                      ))}
                    </div>
                  ) : null}
                </div>
              </aside>
            </Panel>
          ) : null}
        </PanelGroup>
      </div>
    </div>
  );
}

function PanelRightOpenIcon() {
  return <PanelLeftOpen className="h-4 w-4" />;
}
