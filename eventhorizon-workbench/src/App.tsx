import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import ReactMarkdown from "react-markdown";
import { formatDistanceToNow } from "date-fns";
import { Loader2, PanelLeftOpen, PanelRightClose, Play, Square, Wifi, WifiOff } from "lucide-react";
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
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    localStorage.setItem(rightPaneKey, rightPaneCollapsed ? "true" : "false");
  }, [rightPaneCollapsed]);

  const openSession = useCallback(async (sessionId: string) => {
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
  // subscribeToRun is declared below and intentionally omitted to avoid recreating the loader on every event-subscription change.
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [setChanges, setContextView, setCurrentDiff, setCurrentRun, setCurrentSession, setSelectedFile]);

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
                // ignore transient fetch failures while the run is still streaming
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
  const canSubmit = composerValue.trim().length > 0 && connectionStatus !== "disconnected" && currentRun?.status !== "running";

  return (
    <div className="flex min-h-screen flex-col bg-background text-foreground">
      <header className="border-b border-border bg-card/90 px-4 py-3 backdrop-blur">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <div className="text-lg font-semibold">Event Horizon Workbench</div>
            <div className="text-xs text-muted-foreground">
              {statusLabel} · {phase} · {currentRun?.createdAt ? formatDistanceToNow(new Date(currentRun.createdAt), { addSuffix: true }) : "Idle"}
            </div>
          </div>
          <div className="flex items-center gap-3">
            <div className="inline-flex items-center gap-2 rounded-full border border-border px-3 py-1 text-xs text-muted-foreground">
              {connectionStatus === "connected" ? <Wifi className="h-3.5 w-3.5 text-emerald-500" /> : <WifiOff className="h-3.5 w-3.5 text-amber-500" />}
              <span>{connectionStatus}</span>
            </div>
            {currentRun?.status === "running" ? (
              <button type="button" onClick={handleCancel} className="inline-flex items-center gap-2 rounded-full bg-destructive px-3 py-2 text-xs font-medium text-destructive-foreground">
                <Square className="h-3.5 w-3.5" />
                Cancel
              </button>
            ) : null}
            <ThemeToggle />
            <button type="button" onClick={toggleRightPane} className="rounded-full border border-border p-2 text-muted-foreground hover:bg-muted">
              {rightPaneCollapsed ? <PanelRightOpenIcon /> : <PanelRightClose className="h-4 w-4" />}
            </button>
          </div>
        </div>
      </header>

      <div className="flex min-h-0 flex-1">
        <aside className="hidden w-[260px] shrink-0 border-r border-border bg-card lg:flex lg:flex-col">
          <div className="flex items-center justify-between px-4 py-3">
            <div className="text-sm font-medium">Sessions</div>
            <button
              type="button"
              onClick={() => setCurrentSession(undefined)}
              className="rounded-md bg-primary px-3 py-1.5 text-xs text-primary-foreground"
            >
              New Chat
            </button>
          </div>
          <div className="flex-1 overflow-y-auto px-2 pb-3">
            {sessions.length === 0 ? <div className="rounded-lg border border-dashed border-border p-4 text-sm text-muted-foreground">No sessions yet.</div> : null}
            <div className="space-y-1">
              {sessions.map((session) => (
                <button
                  key={session.id}
                  type="button"
                  onClick={() => void openSession(session.id)}
                  className={cn(
                    "w-full rounded-xl border px-3 py-3 text-left transition-colors",
                    currentSession?.id === session.id ? "border-primary bg-primary/10" : "border-transparent hover:border-border hover:bg-muted/70",
                  )}
                >
                  <div className="truncate text-sm font-medium">{session.title}</div>
                  <div className="mt-1 text-xs text-muted-foreground">
                    {formatDistanceToNow(new Date(session.updatedAt), { addSuffix: true })} · {session.status}
                  </div>
                </button>
              ))}
            </div>
          </div>
        </aside>

        <PanelGroup direction="horizontal" className="min-h-0 flex-1">
          <Panel defaultSize={rightPaneCollapsed ? 100 : 62} minSize={45}>
            <main className="flex h-full min-h-0 flex-col bg-background">
              <div className="flex-1 overflow-y-auto px-4 py-4 sm:px-6">
                {!currentSession?.messages.length ? (
                  <div className="mx-auto max-w-3xl rounded-2xl border border-dashed border-border bg-card p-8 text-center shadow-panel">
                    <h1 className="text-2xl font-semibold">Event Horizon Workbench</h1>
                    <p className="mt-3 text-sm text-muted-foreground">
                      Ask the agent to inspect, modify, explain, test, or refactor your code.
                    </p>
                    <div className="mt-6 space-y-2 text-left text-sm text-muted-foreground">
                      <div>- Replace CLI/TUI with an AG-UI web interface</div>
                      <div>- Add tests for EventHorizon</div>
                      <div>- Explain why the build is failing</div>
                      <div>- Refactor this service</div>
                    </div>
                  </div>
                ) : (
                  <div className="mx-auto flex max-w-3xl flex-col gap-4">
                    {currentSession.messages.map((message) => (
                      <div key={message.id} className={cn("rounded-2xl border p-4 shadow-panel", message.role === "user" ? "ml-auto max-w-[85%] bg-primary text-primary-foreground" : "bg-card") }>
                        <div className="mb-2 text-[11px] uppercase tracking-wide opacity-70">{message.role}</div>
                        {message.role === "assistant" ? (
                          <div className="markdown">
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
                      <div className="rounded-2xl border border-primary/20 bg-card p-4 shadow-panel">
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
                          <button type="button" onClick={() => setContextView("files")} className="rounded-md border border-border px-3 py-1.5 text-xs hover:bg-muted">
                            View files
                          </button>
                          <button type="button" onClick={() => setContextView("logs")} className="rounded-md border border-border px-3 py-1.5 text-xs hover:bg-muted">
                            View logs
                          </button>
                        </div>
                      </div>
                    ) : null}
                  </div>
                )}
              </div>

              <div className="border-t border-border bg-card px-4 py-4 sm:px-6">
                <div className="mx-auto max-w-3xl">
                  <div className="rounded-2xl border border-border bg-background p-3 shadow-panel">
                    <textarea
                      value={composerValue}
                      onChange={(event) => setComposerValue(event.target.value)}
                      onKeyDown={(event) => {
                        if ((event.metaKey || event.ctrlKey) && event.key === "Enter") {
                          event.preventDefault();
                          void handleSubmit();
                        }
                      }}
                      placeholder="Ask the agent to change, explain, test, or refactor your code..."
                      className="min-h-28 w-full resize-none bg-transparent text-sm outline-none placeholder:text-muted-foreground"
                    />
                    <div className="mt-3 flex items-center justify-between">
                      <div className="text-xs text-muted-foreground">⌘ / Ctrl + Enter to run</div>
                      <button
                        type="button"
                        onClick={() => void handleSubmit()}
                        disabled={!canSubmit || isSubmitting}
                        className="inline-flex items-center gap-2 rounded-full bg-primary px-4 py-2 text-sm font-medium text-primary-foreground disabled:cursor-not-allowed disabled:opacity-50"
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

          {!rightPaneCollapsed ? <PanelResizeHandle className="w-px bg-border" /> : null}
          {!rightPaneCollapsed ? (
            <Panel defaultSize={38} minSize={25}>
              <aside className="flex h-full min-h-0 flex-col border-l border-border bg-card">
                <div className="flex items-center justify-between border-b border-border px-4 py-3">
                  <div className="flex gap-1 rounded-full bg-muted p-1 text-xs">
                    {(["overview", "files", "diff", "logs"] as const).map((view) => (
                      <button
                        key={view}
                        type="button"
                        onClick={() => setContextView(view)}
                        className={cn(
                          "rounded-full px-3 py-1.5 capitalize",
                          contextView === view ? "bg-card text-foreground shadow-sm" : "text-muted-foreground",
                        )}
                      >
                        {view}
                      </button>
                    ))}
                  </div>
                  <button type="button" onClick={toggleRightPane} className="rounded-md p-2 text-muted-foreground hover:bg-muted">
                    <PanelRightClose className="h-4 w-4" />
                  </button>
                </div>
                <div className="min-h-0 flex-1 overflow-y-auto p-4">
                  {contextView === "overview" ? (
                    <div className="space-y-4 text-sm">
                      <section className="rounded-xl border border-border p-4">
                        <div className="text-xs uppercase tracking-wide text-muted-foreground">Current task</div>
                        <div className="mt-2 font-medium">{currentRun?.task ?? "No active run"}</div>
                      </section>
                      <section className="rounded-xl border border-border p-4">
                        <div className="text-xs uppercase tracking-wide text-muted-foreground">Status</div>
                        <div className="mt-2 font-medium capitalize">{currentRun?.status ?? "idle"}</div>
                        <div className="mt-1 text-muted-foreground">Phase: {phase}</div>
                      </section>
                      <section className="rounded-xl border border-border p-4">
                        <div className="text-xs uppercase tracking-wide text-muted-foreground">Changes</div>
                        <div className="mt-2 font-medium">{changes.length} files</div>
                      </section>
                    </div>
                  ) : null}

                  {contextView === "files" ? (
                    <div className="space-y-2">
                      {changes.length === 0 ? <div className="rounded-xl border border-dashed border-border p-4 text-sm text-muted-foreground">No file changes yet.</div> : null}
                      {changes.map((change) => (
                        <button
                          key={change.path}
                          type="button"
                          onClick={() => void openDiff(change)}
                          className={cn(
                            "flex w-full items-center justify-between rounded-xl border px-3 py-3 text-left hover:bg-muted",
                            selectedFile === change.path ? "border-primary bg-primary/10" : "border-border",
                          )}
                        >
                          <div>
                            <div className="text-sm font-medium">{change.path}</div>
                            <div className="text-xs text-muted-foreground">{change.status}</div>
                          </div>
                          <div className="text-xs text-muted-foreground">+{change.additions ?? 0} / -{change.deletions ?? 0}</div>
                        </button>
                      ))}
                    </div>
                  ) : null}

                  {contextView === "diff" ? (
                    currentDiff ? (
                      <div className="h-[calc(100vh-11rem)] min-h-[360px]">
                        <DiffViewer {...currentDiff} theme={resolvedTheme === "dark" ? "dark" : "light"} onBack={() => setContextView("files")} />
                      </div>
                    ) : (
                      <div className="rounded-xl border border-dashed border-border p-4 text-sm text-muted-foreground">Select a changed file to inspect the diff.</div>
                    )
                  ) : null}

                  {contextView === "logs" ? (
                    <div className="space-y-2">
                      {logs.length === 0 ? <div className="rounded-xl border border-dashed border-border p-4 text-sm text-muted-foreground">No logs yet.</div> : null}
                      {logs.map((log) => (
                        <div key={log.id} className="rounded-xl border border-border p-3">
                          <div className="flex items-center justify-between gap-3 text-xs text-muted-foreground">
                            <span>{log.type}</span>
                            <span>{formatDistanceToNow(new Date(log.timestamp), { addSuffix: true })}</span>
                          </div>
                          <div className="mt-2 text-sm">{log.summary || eventSummary(log.event)}</div>
                          <details className="mt-3 text-xs text-muted-foreground">
                            <summary className="cursor-pointer">Raw JSON</summary>
                            <pre className="mt-2 overflow-x-auto rounded-lg bg-muted p-3 text-xs">{JSON.stringify(log.event, null, 2)}</pre>
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

