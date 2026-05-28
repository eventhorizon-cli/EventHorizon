import { create } from "zustand";
import type {
  AgentEvent,
  AgentPhase,
  AgentRun,
  AgentSession,
  AgentSessionDetail,
  ConnectionStatus,
  ContextView,
  FileChange,
  FileDiff,
  LogItem,
  ThemeMode,
} from "@/types";

export type WorkbenchState = {
  sessions: AgentSession[];
  currentSession?: AgentSessionDetail;
  currentRun?: AgentRun;
  phase: AgentPhase;
  connectionStatus: ConnectionStatus;
  contextView: ContextView;
  rightPaneCollapsed: boolean;
  selectedFile?: string;
  changes: FileChange[];
  currentDiff?: FileDiff;
  logs: LogItem[];
  themeMode: ThemeMode;
  setSessions: (sessions: AgentSession[]) => void;
  setCurrentSession: (session?: AgentSessionDetail) => void;
  setCurrentRun: (run?: AgentRun) => void;
  setPhase: (phase: AgentPhase) => void;
  setConnectionStatus: (status: ConnectionStatus) => void;
  setContextView: (view: ContextView) => void;
  toggleRightPane: () => void;
  setSelectedFile: (path?: string) => void;
  setChanges: (changes: FileChange[]) => void;
  setCurrentDiff: (diff?: FileDiff) => void;
  addLog: (event: AgentEvent) => void;
  appendAssistantDelta: (sessionId: string, delta: string) => void;
  finishAssistantMessage: (sessionId: string, content: string) => void;
  addUserMessage: (sessionId: string, content: string) => void;
  setThemeMode: (mode: ThemeMode) => void;
};

const themeStorageKey = "event-horizon-workbench-theme";

function inferPhase(event: AgentEvent): AgentPhase | undefined {
  if (event.type === "runStarted") return "planning";
  if (event.type === "runFinished") return "completed";
  if (event.type === "runError") return "failed";
  if (event.type === "runCancelled") return "cancelled";
  if (event.type === "toolCallStart") return "editing";
  if (event.type === "command.started") return "validating";
  if (event.type === "plan.updated") return "planning";
  return undefined;
}

function logSummary(event: AgentEvent) {
  return event.text ?? event.error ?? event.type;
}

export const useWorkbenchStore = create<WorkbenchState>((set) => ({
  sessions: [],
  phase: "idle",
  connectionStatus: "connecting",
  contextView: "overview",
  rightPaneCollapsed: false,
  changes: [],
  logs: [],
  themeMode: (localStorage.getItem(themeStorageKey) as ThemeMode | null) ?? "system",
  setSessions: (sessions) => set({ sessions }),
  setCurrentSession: (currentSession) => set({ currentSession }),
  setCurrentRun: (currentRun) => set({ currentRun }),
  setPhase: (phase) => set({ phase }),
  setConnectionStatus: (connectionStatus) => set({ connectionStatus }),
  setContextView: (contextView) => set({ contextView }),
  toggleRightPane: () => set((state) => ({ rightPaneCollapsed: !state.rightPaneCollapsed })),
  setSelectedFile: (selectedFile) => set({ selectedFile }),
  setChanges: (changes) => set({ changes }),
  setCurrentDiff: (currentDiff) => set({ currentDiff }),
  addLog: (event) =>
    set((state) => ({
      logs: [
        ...state.logs,
        {
          id: `${event.runId}-${event.sequence ?? state.logs.length + 1}`,
          timestamp: event.createdAt ?? new Date().toISOString(),
          type: event.type,
          summary: logSummary(event),
          event,
        },
      ],
      phase: inferPhase(event) ?? state.phase,
    })),
  appendAssistantDelta: (sessionId, delta) =>
    set((state) => {
      if (!state.currentSession || state.currentSession.id !== sessionId) {
        return state;
      }

      const messages = [...state.currentSession.messages];
      const last = messages.at(-1);
      if (last?.role === "assistant" && last.status === "streaming") {
        last.content += delta;
      } else {
        messages.push({
          id: `msg_${crypto.randomUUID()}`,
          sessionId,
          role: "assistant",
          content: delta,
          createdAt: new Date().toISOString(),
          status: "streaming",
        });
      }

      return { currentSession: { ...state.currentSession, messages } };
    }),
  finishAssistantMessage: (sessionId, content) =>
    set((state) => {
      if (!state.currentSession || state.currentSession.id !== sessionId) {
        return state;
      }

      const messages = [...state.currentSession.messages];
      const last = messages.at(-1);
      if (last?.role === "assistant") {
        last.content = content;
        last.status = "completed";
      }

      return { currentSession: { ...state.currentSession, messages } };
    }),
  addUserMessage: (sessionId, content) =>
    set((state) => {
      if (!state.currentSession || state.currentSession.id !== sessionId) {
        return state;
      }

      return {
        currentSession: {
          ...state.currentSession,
          messages: [
            ...state.currentSession.messages,
            {
              id: `msg_${crypto.randomUUID()}`,
              sessionId,
              role: "user",
              content,
              createdAt: new Date().toISOString(),
              status: "completed",
            },
          ],
        },
      };
    }),
  setThemeMode: (themeMode) => {
    localStorage.setItem(themeStorageKey, themeMode);
    set({ themeMode });
  },
}));

