import { apiRequest } from "@/api/client";
import type { AgentSession, AgentSessionDetail, AgentWorkspace, DirectoryItem, DirectoryListing, SessionModelSelection } from "@/types";

type SessionPayload = {
  id: string;
  title?: string;
  name?: string;
  status: AgentSession["status"];
  createdAt: string;
  updatedAt: string;
  providerName?: string;
  providerType?: string;
  model?: string;
  lastRunId?: string;
  summary?: string;
  changedFilesCount?: number;
  isTitleGenerated?: boolean;
  workspaceId?: string;
  workspaceName?: string;
  workspaceRoot?: string;
};

type SessionDetailPayload = SessionPayload & {
  messages?: AgentSessionDetail["messages"];
  workspaceSkills?: AgentSessionDetail["workspaceSkills"];
};

type WorkspacePayload = {
  id: string;
  name: string;
  workspaceRoot: string;
  createdAt: string;
  updatedAt: string;
  sessionCount: number;
};

function mapSession(payload: SessionPayload): AgentSession {
  return {
    id: payload.id,
    title: payload.title ?? payload.name ?? "New conversation",
    status: payload.status,
    createdAt: payload.createdAt,
    updatedAt: payload.updatedAt,
    providerName: payload.providerName,
    providerType: payload.providerType,
    model: payload.model,
    lastRunId: payload.lastRunId,
    summary: payload.summary,
    changedFilesCount: payload.changedFilesCount,
    isTitleGenerated: payload.isTitleGenerated,
    workspaceId: payload.workspaceId,
    workspaceName: payload.workspaceName,
    workspaceRoot: payload.workspaceRoot,
  };
}

export async function getSessions(): Promise<AgentSession[]> {
  const payload = await apiRequest<SessionPayload[]>("/api/sessions");
  return payload.map(mapSession);
}

function mapWorkspace(payload: WorkspacePayload): AgentWorkspace {
  return {
    id: payload.id,
    name: payload.name,
    workspaceRoot: payload.workspaceRoot,
    createdAt: payload.createdAt,
    updatedAt: payload.updatedAt,
    sessionCount: payload.sessionCount,
  };
}

export async function getWorkspaces(): Promise<AgentWorkspace[]> {
  const payload = await apiRequest<WorkspacePayload[]>("/api/workspaces");
  return payload.map(mapWorkspace);
}

export async function getDirectories(path?: string): Promise<DirectoryListing> {
  const params = path ? new URLSearchParams({ path }) : undefined;
  const url = path ? `/api/sessions/directories?${params}` : "/api/sessions/directories";
  return apiRequest<DirectoryListing>(url);
}

export async function createDirectory(input: {
  parentPath?: string;
  name: string;
}): Promise<DirectoryItem> {
  return apiRequest<DirectoryItem>("/api/sessions/directories", {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export async function createWorkspace(input: {
  workspaceRoot?: string;
}): Promise<AgentWorkspace> {
  return mapWorkspace(
    await apiRequest<WorkspacePayload>("/api/workspaces", {
      method: "POST",
      body: JSON.stringify(input),
    }),
  );
}

export async function createSession(input: {
  workspaceId: string;
  initialMessage?: string;
  providerName?: string;
  model?: string;
}): Promise<AgentSession> {
  return mapSession(
    await apiRequest<SessionPayload>(`/api/workspaces/${encodeURIComponent(input.workspaceId)}/sessions`, {
      method: "POST",
      body: JSON.stringify({
        initialMessage: input.initialMessage,
        providerName: input.providerName,
        model: input.model,
      }),
    }),
  );
}

export async function getSession(sessionId: string): Promise<AgentSessionDetail> {
  const payload = await apiRequest<SessionDetailPayload>(`/api/sessions/${sessionId}`);
  return {
    ...mapSession(payload),
    messages: payload.messages ?? [],
    workspaceSkills: payload.workspaceSkills ?? { imported: [] },
  };
}

export async function updateSession(input: {
  sessionId: string;
  title?: string;
  providerName?: string | null;
  model?: string | null;
}): Promise<AgentSession> {
  return mapSession(
    await apiRequest<SessionPayload>(`/api/sessions/${input.sessionId}`, {
      method: "PATCH",
      body: JSON.stringify({
        title: input.title,
        providerName: input.providerName,
        model: input.model,
      }),
    }),
  );
}

export async function updateSessionModel(input: {
  sessionId: string;
  providerName?: string | null;
  modelId?: string | null;
}): Promise<SessionModelSelection> {
  return apiRequest<SessionModelSelection>(`/api/sessions/${input.sessionId}/model`, {
    method: "PUT",
    body: JSON.stringify({
      providerName: input.providerName,
      modelId: input.modelId,
    }),
  });
}

export async function deleteSession(sessionId: string): Promise<void> {
  await apiRequest(`/api/sessions/${sessionId}`, { method: "DELETE" });
}

export async function deleteWorkspace(workspaceId: string): Promise<void> {
  await apiRequest(`/api/workspaces/${encodeURIComponent(workspaceId)}`, { method: "DELETE" });
}
