export type RunStatus = "idle" | "running" | "completed" | "failed" | "cancelled";
export type SessionStatus = RunStatus;
export type ThemeMode = "light" | "dark" | "system";
export type ConnectionStatus = "connecting" | "connected" | "reconnecting" | "disconnected";
export type ContextView = "overview" | "files" | "diff" | "logs" | "details";
export type AgentPhase = "idle" | "understanding" | "inspecting" | "planning" | "editing" | "validating" | "summarizing" | "completed" | "failed" | "cancelled";
export type FileChangeStatus = "added" | "modified" | "deleted" | "renamed";

export type DirectoryItem = {
  path: string;
  name: string;
  isDirectory: boolean;
  parentPath?: string;
};

export type AgentSession = {
  id: string;
  title: string;
  status: SessionStatus;
  createdAt: string;
  updatedAt: string;
  lastRunId?: string;
  summary?: string;
  changedFilesCount?: number;
  isTitleGenerated?: boolean;
  workspaceRoot?: string;
};

export type ChatMessage = {
  id: string;
  sessionId: string;
  role: "user" | "assistant" | "system";
  content: string;
  createdAt: string;
  status?: "streaming" | "completed" | "failed";
};

export type AgentSessionDetail = AgentSession & {
  messages: ChatMessage[];
};

export type AgentRun = {
  id: string;
  sessionId?: string;
  status: RunStatus;
  task: string;
  createdAt: string;
  updatedAt?: string;
  detailedStatus?: string;
  error?: string;
};

export type AgentEvent = {
  sequence?: number;
  type: string;
  runId: string;
  threadId: string;
  createdAt?: string;
  status?: string;
  messageId?: string;
  toolCallId?: string;
  toolCallName?: string;
  stepId?: string;
  artifactId?: string;
  delta?: string;
  text?: string;
  error?: string;
  message?: unknown;
  toolCall?: unknown;
  result?: unknown;
  artifact?: unknown;
  summary?: unknown;
  state?: unknown;
  metadata?: unknown;
};

export type FileChange = {
  path: string;
  oldPath?: string;
  status: FileChangeStatus;
  additions?: number;
  deletions?: number;
  binary?: boolean;
};

export type FileDiff = {
  path: string;
  oldPath?: string;
  status: FileChangeStatus;
  oldText?: string;
  newText?: string;
  language?: string;
  binary?: boolean;
  additions?: number;
  deletions?: number;
};

export type LogItem = {
  id: string;
  timestamp: string;
  type: string;
  summary: string;
  event: AgentEvent;
};

