export type RunStatus = "idle" | "running" | "completed" | "failed" | "cancelled";
export type SessionStatus = RunStatus;
export type ThemeMode = "light" | "dark" | "system";
export type ConnectionStatus = "connecting" | "connected" | "reconnecting" | "disconnected";
export type ContextView = "overview" | "files" | "diff" | "settings";
export type AgentPhase = "idle" | "understanding" | "inspecting" | "planning" | "editing" | "validating" | "summarizing" | "completed" | "failed" | "cancelled";
export type FileChangeStatus = "added" | "modified" | "deleted" | "renamed";
export type ProviderType = "openai" | "openai-compatible" | "azure-openai" | "anthropic" | "gemini";

export type DirectoryItem = {
  path: string;
  name: string;
  isDirectory: boolean;
  parentPath?: string;
};

export type DirectoryListing = {
  currentPath: string;
  items: DirectoryItem[];
};

export type ProviderConfig = {
  type?: ProviderType;
  model?: string;
  models: string[];
  endpoint?: string;
  apiKey?: string;
  deployment?: string;
  useDefaultAzureCredential: boolean;
};

export type ProviderEntry = {
  name: string;
  provider: ProviderConfig;
};

export type McpServerConfig = {
  enabled: boolean;
  name?: string;
  url: string;
  headers: Record<string, string>;
};

export type ImportedSkill = {
  enabled: boolean;
  name: string;
  path: string;
  description?: string;
  importedAt?: string;
};

export type SkillCatalog = {
  storagePath?: string;
  imported: ImportedSkill[];
};

export type AppConfiguration = {
  filePath: string;
  currentDefaultProvider?: string;
  providers: ProviderEntry[];
  mcpServers: McpServerConfig[];
  skills: SkillCatalog;
};

export type ProviderTestResult = {
  success: boolean;
  message: string;
  models: string[];
};

export type McpTestResult = {
  success: boolean;
  message: string;
  tools: string[];
};

export type SkillImportResult = {
  success: boolean;
  message: string;
  skill?: ImportedSkill;
  errors: string[];
};

export type AgentSession = {
  id: string;
  title: string;
  status: SessionStatus;
  createdAt: string;
  updatedAt: string;
   providerName?: string;
   providerType?: string;
   model?: string;
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
  sessionSkills: SkillCatalog;
};

export type AgentRun = {
  id: string;
  threadId: string;
  sessionId: string;
  status: RunStatus;
  task: string;
  workingDirectory?: string;
  providerName?: string;
  model?: string;
  createdAt: string;
  updatedAt?: string;
  detailedStatus?: string;
  error?: string;
};

export type ReasoningSummary = {
  goal: string;
  plan: string[];
  completed: string[];
  next?: string | null;
  issues: string[];
  decisions: string[];
};

export type ToolCallDescriptor = {
  id: string;
  name: string;
  arguments?: string | null;
  status: string;
  result?: string | null;
};

export type SessionModelSelection = {
  sessionId: string;
  providerName?: string;
  providerType: string;
  modelId: string;
  warnings: string[];
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
  toolCall?: ToolCallDescriptor | unknown;
  result?: unknown;
  artifact?: unknown;
  summary?: ReasoningSummary;
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
