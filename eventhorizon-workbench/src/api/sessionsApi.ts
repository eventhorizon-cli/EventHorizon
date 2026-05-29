import { apiRequest } from "@/api/client";
import type { AgentSession, AgentSessionDetail, DirectoryItem } from "@/types";

type SessionPayload = {
  id: string;
  title?: string;
  name?: string;
  status: AgentSession["status"];
  createdAt: string;
  updatedAt: string;
  lastRunId?: string;
  summary?: string;
  changedFilesCount?: number;
  isTitleGenerated?: boolean;
};

type SessionDetailPayload = SessionPayload & {
  messages?: AgentSessionDetail["messages"];
};

function mapSession(payload: SessionPayload): AgentSession {
  return {
    id: payload.id,
    title: payload.title ?? payload.name ?? "New conversation",
    status: payload.status,
    createdAt: payload.createdAt,
    updatedAt: payload.updatedAt,
    lastRunId: payload.lastRunId,
    summary: payload.summary,
    changedFilesCount: payload.changedFilesCount,
    isTitleGenerated: payload.isTitleGenerated,
    workspaceRoot: (payload as any).workspaceRoot,
  };
}

export async function getSessions(): Promise<AgentSession[]> {
  const payload = await apiRequest<SessionPayload[]>("/api/sessions");
  return payload.map(mapSession);
}

export async function getDirectories(path?: string): Promise<DirectoryItem[]> {
  const params = path ? new URLSearchParams({ path }) : undefined;
  const url = path ? `/api/directories?${params}` : "/api/directories";
  return apiRequest<DirectoryItem[]>(url);
}

export async function createSession(initialMessage?: string, workspaceRoot?: string): Promise<AgentSession> {
  return mapSession(
    await apiRequest<SessionPayload>("/api/sessions", {
      method: "POST",
      body: JSON.stringify({ initialMessage, workspaceRoot }),
    }),
  );
}

export async function getSession(sessionId: string): Promise<AgentSessionDetail> {
  const payload = await apiRequest<SessionDetailPayload>(`/api/sessions/${sessionId}`);
  return {
    ...mapSession(payload),
    messages: payload.messages ?? [],
  };
}

export async function updateSessionTitle(sessionId: string, title: string): Promise<AgentSession> {
  return mapSession(
    await apiRequest<SessionPayload>(`/api/sessions/${sessionId}`, {
      method: "PATCH",
      body: JSON.stringify({ title }),
    }),
  );
}

export async function deleteSession(sessionId: string): Promise<void> {
  await apiRequest(`/api/sessions/${sessionId}`, { method: "DELETE" });
}

